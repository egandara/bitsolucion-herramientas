using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotebookValidator.Web.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NotebookValidator.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class GestorDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GestorDashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Helper para saltar fines de semana y feriados
        private bool EsHabil(DateTime date, List<DateTime> feriados)
        {
            return date.DayOfWeek != DayOfWeek.Saturday &&
                   date.DayOfWeek != DayOfWeek.Sunday &&
                   !feriados.Contains(date.Date);
        }

        public async Task<IActionResult> Index()
        {
            // ==========================================
            // 1. TARJETAS SUPERIORES (KPIs VITALES)
            // ==========================================
            var proyectosEnRiesgoList = await _context.Proyectos
                .Where(p => p.Estado == "Activo" && p.FechaFinEstimada.HasValue && p.FechaFinEstimada.Value < DateTime.Now)
                .Select(p => new { p.Nombre, p.FechaFinEstimada })
                .OrderBy(p => p.FechaFinEstimada)
                .ToListAsync();

            var totalValidaciones = await _context.NotebookValidaciones.CountAsync();
            var validacionesAprobadas = await _context.NotebookValidaciones.CountAsync(v => v.PasoValidacion);
            double tasaExitoQA = totalValidaciones > 0 ? Math.Round((double)validacionesAprobadas / totalValidaciones * 100, 1) : 0;

            var sobrecargaActual = await CalcularSobrecarga(0);

            var alertasVencidas = await _context.ComentariosProyecto
                .CountAsync(c => !c.Resuelto && c.Tipo == "Recordatorio" && c.FechaVencimiento.HasValue && c.FechaVencimiento.Value < DateTime.Now);

            ViewBag.ProyectosEnRiesgo = proyectosEnRiesgoList.Count;
            ViewBag.ProyectosEnRiesgoList = proyectosEnRiesgoList;
            ViewBag.TasaExitoQA = tasaExitoQA;
            ViewBag.SobrecargaCount = sobrecargaActual.Count;
            ViewBag.SobrecargaLabel = sobrecargaActual.Label;
            ViewBag.SobrecargaRango = sobrecargaActual.Rango;
            ViewBag.AlertasVencidas = alertasVencidas;

            // ==========================================
            // 2. GRÁFICO 1: DESVIACIÓN DE ESFUERZO
            // ==========================================
            var proyectosEsfuerzo = await _context.Proyectos
                .Include(p => p.Fases).ThenInclude(f => f.SubFases).ThenInclude(s => s.Tareas)
                .Where(p => p.Estado == "Activo" || p.Estado == "Finalizado")
                .OrderByDescending(p => p.FechaCreacion)
                .Take(5)
                .ToListAsync();

            var labelsEsfuerzo = proyectosEsfuerzo.Select(p => p.Nombre).ToList();
            var dataPlanificada = proyectosEsfuerzo.Select(p =>
                p.Fases?.SelectMany(f => f.SubFases ?? Enumerable.Empty<NotebookValidator.Web.Models.GestorProyectos.SubFaseProyecto>())
                       .SelectMany(s => s.Tareas ?? Enumerable.Empty<NotebookValidator.Web.Models.GestorProyectos.TareaProyecto>())
                       .Sum(t => t.HorasEstimadas) ?? 0).ToList();

            var dataReal = proyectosEsfuerzo.Select(p =>
                p.Fases?.SelectMany(f => f.SubFases ?? Enumerable.Empty<NotebookValidator.Web.Models.GestorProyectos.SubFaseProyecto>())
                       .SelectMany(s => s.Tareas ?? Enumerable.Empty<NotebookValidator.Web.Models.GestorProyectos.TareaProyecto>())
                       .Where(t => t.Estado == "Terminada")
                       .Sum(t => t.HorasRealesDeducidas) ?? 0).ToList();

            ViewBag.LabelsEsfuerzo = System.Text.Json.JsonSerializer.Serialize(labelsEsfuerzo);
            ViewBag.DataPlanificada = System.Text.Json.JsonSerializer.Serialize(dataPlanificada);
            ViewBag.DataReal = System.Text.Json.JsonSerializer.Serialize(dataReal);

            // ==========================================
            // 3A. GRÁFICO 2 (HISTÓRICO): CARGA DIARIA POR USUARIO (Últimos 14 días naturales)
            // ==========================================
            DateTime catorceDiasAtras = DateTime.Today.AddDays(-13);
            var tareasCompletadas = await _context.TareasProyecto
                .Include(t => t.UsuarioAsignado)
                .Where(t => t.Estado == "Terminada" && t.FechaFinReal.HasValue && t.FechaFinReal.Value.Date >= catorceDiasAtras)
                .ToListAsync();

            var diasPasados = Enumerable.Range(0, 14).Select(i => catorceDiasAtras.AddDays(i).Date).ToList();
            var etiquetasDiasPasados = diasPasados.Select(d => d.ToString("dd MMM")).ToList();
            var usuariosPasado = tareasCompletadas.Where(t => t.UsuarioAsignado != null).Select(t => t.UsuarioAsignado.Email).Distinct().ToList();
            var seriesCargaPasado = new List<object>();

            foreach (var userEmail in usuariosPasado)
            {
                var dataUsuario = new List<decimal>();
                foreach (var dia in diasPasados)
                {
                    var horasDia = tareasCompletadas.Where(t => t.UsuarioAsignado?.Email == userEmail && t.FechaFinReal?.Date == dia).Sum(t => t.HorasRealesDeducidas);
                    dataUsuario.Add(Math.Round(horasDia, 1));
                }

                // Solo agregar si el usuario tiene datos (evita leyendas vacías)
                if (dataUsuario.Any(h => h > 0))
                {
                    seriesCargaPasado.Add(new { name = userEmail.Split('@')[0], data = dataUsuario });
                }
            }

            ViewBag.EtiquetasDias = System.Text.Json.JsonSerializer.Serialize(etiquetasDiasPasados);
            ViewBag.SeriesCarga = System.Text.Json.JsonSerializer.Serialize(seriesCargaPasado);

            // ==========================================
            // 3B. GRÁFICO 2 (PROYECCIÓN): PRÓXIMOS 14 DÍAS HÁBILES
            // ==========================================
            var feriadosDb = await _context.Feriados.Where(f => f.Activo).Select(f => f.Fecha.Date).ToListAsync();

            var proximosDiasHabiles = new List<DateTime>();
            DateTime cursor = DateTime.Today;
            while (proximosDiasHabiles.Count < 14)
            {
                if (EsHabil(cursor, feriadosDb)) proximosDiasHabiles.Add(cursor);
                cursor = cursor.AddDays(1);
            }

            var etiquetasDiasFuturo = proximosDiasHabiles.Select(d => d.ToString("dd MMM")).ToList();
            DateTime maxFutureDate = proximosDiasHabiles.Last();

            // 🎯 CORRECCIÓN: Traemos TODAS las pendientes, incluso sin asignar
            var todasLasTareasPendientes = await _context.TareasProyecto
                .Include(t => t.UsuarioAsignado)
                .Where(t => t.Estado != "Terminada")
                .ToListAsync();

            var seriesCargaFuturo = new List<object>();

            if (todasLasTareasPendientes.Any())
            {
                // 🎯 CORRECCIÓN: Agrupar por correo o bolsa de "Sin Asignar"
                var usuariosFuturo = todasLasTareasPendientes
                    .Select(t => t.UsuarioAsignado?.Email ?? "Sin Asignar")
                    .Distinct()
                    .ToList();

                foreach (var userEmail in usuariosFuturo)
                {
                    var dataUsuarioFuturo = new decimal[14];
                    var tareasDelUsuario = todasLasTareasPendientes
                        .Where(t => (t.UsuarioAsignado?.Email ?? "Sin Asignar") == userEmail)
                        .ToList();

                    foreach (var t in tareasDelUsuario)
                    {
                        DateTime startOriginal = t.FechaInicioReal ?? t.FechaInicioPlanificada ?? t.FechaCreacion;
                        decimal horasEstimadas = t.HorasEstimadas > 0 ? t.HorasEstimadas : 1m;
                        DateTime endOriginal = t.FechaFinPlanificada ?? t.FechaFinReal ?? startOriginal.AddHours((double)horasEstimadas);

                        // 🎯 REGLA DE PM: Si está atrasada, el esfuerzo recae desde HOY
                        DateTime startTask = startOriginal.Date < DateTime.Today ? DateTime.Today : startOriginal.Date;
                        DateTime endTask = endOriginal.Date < DateTime.Today ? DateTime.Today : endOriginal.Date;

                        if (endTask < startTask) endTask = startTask;

                        // Si la tarea arranca después de nuestros 14 días proyectados, se omite
                        if (startTask > maxFutureDate) continue;

                        int taskWorkingDays = 0;
                        for (var d = startTask; d <= endTask; d = d.AddDays(1))
                        {
                            if (EsHabil(d, feriadosDb)) taskWorkingDays++;
                        }

                        // Si la tarea se programó 100% en finde/feriado, forzar 1 día para no borrar las horas
                        if (taskWorkingDays == 0) taskWorkingDays = 1;

                        decimal hoursPerDay = horasEstimadas / taskWorkingDays;

                        for (int i = 0; i < 14; i++)
                        {
                            var diaHabil = proximosDiasHabiles[i];
                            // Solo cobramos las horas si el día cae en el rango vital de la tarea
                            if (diaHabil >= startTask && diaHabil <= endTask)
                            {
                                dataUsuarioFuturo[i] += hoursPerDay;
                            }
                        }
                    }

                    // Agregar al gráfico solo si tiene alguna hora proyectada
                    if (dataUsuarioFuturo.Any(h => h > 0))
                    {
                        seriesCargaFuturo.Add(new
                        {
                            name = userEmail == "Sin Asignar" ? "Sin Asignar" : userEmail.Split('@')[0],
                            data = dataUsuarioFuturo.Select(d => Math.Round(d, 1)).ToList()
                        });
                    }
                }
            }

            ViewBag.EtiquetasDiasFuturo = System.Text.Json.JsonSerializer.Serialize(etiquetasDiasFuturo);
            ViewBag.SeriesCargaFuturo = System.Text.Json.JsonSerializer.Serialize(seriesCargaFuturo);

            // ==========================================
            // 4. LISTA DE ACCIÓN (TAREAS ESTANCADAS)
            // ==========================================
            var tareasEstancadas = await _context.TareasProyecto
                .Include(t => t.SubFase).ThenInclude(s => s.Fase).ThenInclude(f => f.Proyecto)
                .Include(t => t.UsuarioAsignado)
                .Where(t => t.Estado == "En Progreso" && t.FechaInicioReal.HasValue)
                .OrderBy(t => t.FechaInicioReal)
                .Take(6)
                .AsNoTracking()
                .ToListAsync();

            ViewBag.TareasEstancadas = tareasEstancadas;

            return View();
        }

        // ==========================================
        // AJAX: ENDPOINT PARA NAVEGAR ENTRE SEMANAS
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> GetSobrecargaPorSemana(int offset = 0)
        {
            var resultado = await CalcularSobrecarga(offset);
            return Json(resultado);
        }

        private async Task<SobrecargaResult> CalcularSobrecarga(int offsetSemanas)
        {
            DateTime fechaBase = DateTime.Today.AddDays(offsetSemanas * 7);
            int diff = (7 + (fechaBase.DayOfWeek - DayOfWeek.Monday)) % 7;
            DateTime inicioSemana = fechaBase.AddDays(-1 * diff).Date;
            DateTime finSemana = inicioSemana.AddDays(6).AddHours(23).AddMinutes(59).AddSeconds(59);

            var tareasPendientes = await _context.TareasProyecto
                .Where(t => t.UsuarioAsignadoId != null && t.Estado != "Terminada")
                .ToListAsync();

            var tareasEnRango = tareasPendientes.Where(t => {
                DateTime start = t.FechaInicioReal ?? t.FechaInicioPlanificada ?? t.FechaCreacion;
                return start >= inicioSemana && start <= finSemana;
            });

            var count = tareasEnRango
                .GroupBy(t => t.UsuarioAsignadoId)
                .Select(g => new { TotalHoras = g.Sum(x => x.HorasEstimadas) })
                .Count(x => x.TotalHoras > 40);

            string label = offsetSemanas == 0 ? "ESTA SEMANA" : (offsetSemanas == 1 ? "PRÓX. SEMANA" : (offsetSemanas == -1 ? "SEM. PASADA" : $"SEMANA {offsetSemanas}"));
            string rango = $"{inicioSemana:dd MMM} - {finSemana:dd MMM}";

            return new SobrecargaResult { Count = count, Label = label, Rango = rango };
        }

        private class SobrecargaResult
        {
            public int Count { get; set; }
            public string Label { get; set; } = string.Empty;
            public string Rango { get; set; } = string.Empty;
        }
    }
}
