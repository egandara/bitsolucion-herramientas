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

        // Objeto unificado para mezclar Tareas y Subfases en el mismo gráfico
        private class WorkItem
        {
            public string Email { get; set; } = "Sin Asignar";
            public DateTime Start { get; set; }
            public DateTime End { get; set; }
            public decimal Horas { get; set; }
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
                       .Sum(s => s.HorasEstimadas + (s.Tareas?.Sum(t => t.HorasEstimadas) ?? 0)) ?? 0).ToList();

            var dataReal = proyectosEsfuerzo.Select(p =>
                p.Fases?.SelectMany(f => f.SubFases ?? Enumerable.Empty<NotebookValidator.Web.Models.GestorProyectos.SubFaseProyecto>())
                       .SelectMany(s => s.Tareas ?? Enumerable.Empty<NotebookValidator.Web.Models.GestorProyectos.TareaProyecto>())
                       .Where(t => t.Estado == "Terminada")
                       .Sum(t => t.HorasRealesDeducidas) ?? 0).ToList();

            ViewBag.LabelsEsfuerzo = System.Text.Json.JsonSerializer.Serialize(labelsEsfuerzo);
            ViewBag.DataPlanificada = System.Text.Json.JsonSerializer.Serialize(dataPlanificada);
            ViewBag.DataReal = System.Text.Json.JsonSerializer.Serialize(dataReal);

            // ==========================================
            // 3A. GRÁFICO 2 (HISTÓRICO): CARGA DIARIA POR USUARIO
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

            // 🎯 FUSIÓN DE TAREAS Y SUBFASES PARA LA PROYECCIÓN
            var tareasPendientes = await _context.TareasProyecto.Include(t => t.UsuarioAsignado).Where(t => t.Estado != "Terminada").ToListAsync();
            var subfasesPendientes = await _context.SubFasesProyecto.Include(s => s.Responsable).Where(s => s.Estado != "Terminada").ToListAsync();

            var workItems = new List<WorkItem>();

            // Procesar Tareas
            foreach (var t in tareasPendientes)
            {
                DateTime s = t.FechaInicioReal ?? t.FechaInicioPlanificada ?? t.FechaCreacion;
                decimal h = t.HorasEstimadas > 0 ? t.HorasEstimadas : 1m;
                DateTime e = t.FechaFinPlanificada ?? t.FechaFinReal ?? s.AddHours((double)h);
                workItems.Add(new WorkItem { Email = t.UsuarioAsignado?.Email ?? "Sin Asignar", Start = s, End = e, Horas = h });
            }

            // Procesar Subfases (Lo que te estaba faltando)
            foreach (var sub in subfasesPendientes)
            {
                if (!sub.FechaInicio.HasValue) continue;
                DateTime s = sub.FechaInicio.Value;
                decimal h = sub.HorasEstimadas > 0 ? sub.HorasEstimadas : 1m;
                DateTime e = sub.FechaFinEstimada ?? s.AddHours((double)h);
                workItems.Add(new WorkItem { Email = sub.Responsable?.Email ?? "Sin Asignar", Start = s, End = e, Horas = h });
            }

            var seriesCargaFuturo = new List<object>();

            if (workItems.Any())
            {
                var usuariosFuturo = workItems.Select(w => w.Email).Distinct().ToList();

                foreach (var userEmail in usuariosFuturo)
                {
                    var dataUsuarioFuturo = new decimal[14];
                    var itemsDelUsuario = workItems.Where(w => w.Email == userEmail).ToList();

                    foreach (var item in itemsDelUsuario)
                    {
                        DateTime startTask = item.Start.Date < DateTime.Today ? DateTime.Today : item.Start.Date;
                        DateTime endTask = item.End.Date < DateTime.Today ? DateTime.Today : item.End.Date;

                        if (endTask < startTask) endTask = startTask;
                        if (startTask > maxFutureDate) continue; // Cae fuera de los 14 días

                        int taskWorkingDays = 0;
                        for (var d = startTask; d <= endTask; d = d.AddDays(1))
                        {
                            if (EsHabil(d, feriadosDb)) taskWorkingDays++;
                        }

                        if (taskWorkingDays == 0) taskWorkingDays = 1; // Seguridad
                        decimal hoursPerDay = item.Horas / taskWorkingDays;

                        for (int i = 0; i < 14; i++)
                        {
                            var diaHabil = proximosDiasHabiles[i];
                            if (diaHabil >= startTask && diaHabil <= endTask)
                            {
                                dataUsuarioFuturo[i] += hoursPerDay;
                            }
                        }
                    }

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

            var tareasPendientes = await _context.TareasProyecto.Where(t => t.UsuarioAsignadoId != null && t.Estado != "Terminada").ToListAsync();
            var subfasesPendientes = await _context.SubFasesProyecto.Where(s => s.ResponsableId != null && s.Estado != "Terminada").ToListAsync();

            var workItems = new List<WorkItem>();

            foreach (var t in tareasPendientes)
            {
                DateTime s = t.FechaInicioReal ?? t.FechaInicioPlanificada ?? t.FechaCreacion;
                workItems.Add(new WorkItem { Email = t.UsuarioAsignadoId, Start = s, Horas = t.HorasEstimadas });
            }
            foreach (var sub in subfasesPendientes)
            {
                if (sub.FechaInicio.HasValue)
                    workItems.Add(new WorkItem { Email = sub.ResponsableId, Start = sub.FechaInicio.Value, Horas = sub.HorasEstimadas });
            }

            var itemsEnRango = workItems.Where(w => w.Start >= inicioSemana && w.Start <= finSemana);

            var count = itemsEnRango
                .GroupBy(w => w.Email)
                .Select(g => new { TotalHoras = g.Sum(x => x.Horas) })
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
