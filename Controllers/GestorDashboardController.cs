using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NotebookValidator.Web.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
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

        private bool EsHabil(DateTime date, List<DateTime> feriados)
        {
            return date.DayOfWeek != DayOfWeek.Saturday &&
                   date.DayOfWeek != DayOfWeek.Sunday &&
                   !feriados.Contains(date.Date);
        }

        private string FormatearDiaSemana(DateTime d)
        {
            var culture = new CultureInfo("es-ES");
            string str = d.ToString("ddd dd MMM", culture).Replace(".", "");
            return char.ToUpper(str[0]) + str.Substring(1);
        }

        private class WorkItem
        {
            public string Email { get; set; } = "Sin Asignar";
            public DateTime Start { get; set; }
            public DateTime End { get; set; }
            public decimal Horas { get; set; }
        }

        public async Task<IActionResult> Index(int? proyectoId = null)
        {
            var listaProyectos = await _context.Proyectos.Where(p => p.Estado != "Eliminado").OrderBy(p => p.Nombre).ToListAsync();
            ViewBag.FiltroProyectos = new SelectList(listaProyectos, "Id", "Nombre", proyectoId);
            ViewBag.ProyectoActivo = proyectoId;

            // ==========================================
            // 1. TARJETAS SUPERIORES (KPIs VITALES)
            // ==========================================
            var proyectosEnRiesgoQuery = _context.Proyectos.Where(p => p.Estado == "Activo" && p.FechaFinEstimada.HasValue && p.FechaFinEstimada.Value < DateTime.Now);
            if (proyectoId.HasValue) proyectosEnRiesgoQuery = proyectosEnRiesgoQuery.Where(p => p.Id == proyectoId.Value);

            var proyectosEnRiesgoList = await proyectosEnRiesgoQuery.Select(p => new { p.Nombre, p.FechaFinEstimada }).OrderBy(p => p.FechaFinEstimada).ToListAsync();

            var valQuery = _context.NotebookValidaciones.AsQueryable();
            if (proyectoId.HasValue) valQuery = valQuery.Where(v => v.ProyectoId == proyectoId.Value);

            var totalValidaciones = await valQuery.CountAsync();
            var validacionesAprobadas = await valQuery.CountAsync(v => v.PasoValidacion);
            double tasaExitoQA = totalValidaciones > 0 ? Math.Round((double)validacionesAprobadas / totalValidaciones * 100, 1) : 0;

            var sobrecargaActual = await CalcularSobrecarga(0, proyectoId);

            var alertasQuery = _context.ComentariosProyecto.Where(c => !c.Resuelto && c.Tipo == "Recordatorio" && c.FechaVencimiento.HasValue && c.FechaVencimiento.Value < DateTime.Now);
            if (proyectoId.HasValue) alertasQuery = alertasQuery.Where(c => c.ProyectoId == proyectoId.Value);
            var alertasVencidas = await alertasQuery.CountAsync();

            ViewBag.ProyectosEnRiesgo = proyectosEnRiesgoList.Count;
            ViewBag.ProyectosEnRiesgoList = proyectosEnRiesgoList;
            ViewBag.TasaExitoQA = tasaExitoQA;
            ViewBag.SobrecargaCount = sobrecargaActual.Count;
            ViewBag.SobrecargaLabel = sobrecargaActual.Label;
            ViewBag.SobrecargaRango = sobrecargaActual.Rango;
            ViewBag.SobrecargaList = sobrecargaActual.Usuarios;
            ViewBag.AlertasVencidas = alertasVencidas;

            // ==========================================
            // 2. GRÁFICO 1: DESVIACIÓN DE ESFUERZO
            // ==========================================
            var esfQuery = _context.Proyectos
                .Include(p => p.Fases).ThenInclude(f => f.SubFases).ThenInclude(s => s.Tareas)
                .Where(p => p.Estado == "Activo" || p.Estado == "Finalizado");

            if (proyectoId.HasValue) esfQuery = esfQuery.Where(p => p.Id == proyectoId.Value);

            var proyectosEsfuerzo = await esfQuery.OrderByDescending(p => p.FechaCreacion).Take(5).ToListAsync();

            var labelsEsfuerzo = proyectosEsfuerzo.Select(p => p.Nombre).ToList();
            var dataPlanificada = proyectosEsfuerzo.Select(p => p.Fases?.SelectMany(f => f.SubFases ?? Enumerable.Empty<NotebookValidator.Web.Models.GestorProyectos.SubFaseProyecto>()).Sum(s => s.HorasEstimadas + (s.Tareas?.Sum(t => t.HorasEstimadas) ?? 0)) ?? 0).ToList();
            var dataReal = proyectosEsfuerzo.Select(p => p.Fases?.SelectMany(f => f.SubFases ?? Enumerable.Empty<NotebookValidator.Web.Models.GestorProyectos.SubFaseProyecto>()).SelectMany(s => s.Tareas ?? Enumerable.Empty<NotebookValidator.Web.Models.GestorProyectos.TareaProyecto>()).Where(t => t.Estado == "Terminada").Sum(t => t.HorasRealesDeducidas) ?? 0).ToList();

            ViewBag.LabelsEsfuerzo = System.Text.Json.JsonSerializer.Serialize(labelsEsfuerzo);
            ViewBag.DataPlanificada = System.Text.Json.JsonSerializer.Serialize(dataPlanificada);
            ViewBag.DataReal = System.Text.Json.JsonSerializer.Serialize(dataReal);

            // ==========================================
            // 3A. GRÁFICO 2: CARGA HISTÓRICA
            // ==========================================
            DateTime catorceDiasAtras = DateTime.Today.AddDays(-13);
            var tarCompQuery = _context.TareasProyecto.Include(t => t.UsuarioAsignado).Include(t => t.SubFase.Fase).Where(t => t.Estado == "Terminada" && t.FechaFinReal.HasValue && t.FechaFinReal.Value.Date >= catorceDiasAtras);
            if (proyectoId.HasValue) tarCompQuery = tarCompQuery.Where(t => t.SubFase.Fase.ProyectoId == proyectoId.Value);
            var tareasCompletadas = await tarCompQuery.ToListAsync();

            var diasPasados = Enumerable.Range(0, 14).Select(i => catorceDiasAtras.AddDays(i).Date).ToList();
            var etiquetasDiasPasados = diasPasados.Select(FormatearDiaSemana).ToList();
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
                if (dataUsuario.Any(h => h > 0)) seriesCargaPasado.Add(new { name = userEmail.Split('@')[0], data = dataUsuario });
            }
            ViewBag.EtiquetasDias = System.Text.Json.JsonSerializer.Serialize(etiquetasDiasPasados);
            ViewBag.SeriesCarga = System.Text.Json.JsonSerializer.Serialize(seriesCargaPasado);

            // ==========================================
            // 3B. GRÁFICO 2: PROYECCIÓN FUTURA
            // ==========================================
            var feriadosDb = await _context.Feriados.Where(f => f.Activo).Select(f => f.Fecha.Date).ToListAsync();
            var proximosDiasHabiles = new List<DateTime>();
            DateTime cursor = DateTime.Today;
            while (proximosDiasHabiles.Count < 14) { if (EsHabil(cursor, feriadosDb)) proximosDiasHabiles.Add(cursor); cursor = cursor.AddDays(1); }
            var etiquetasDiasFuturo = proximosDiasHabiles.Select(FormatearDiaSemana).ToList();
            DateTime maxFutureDate = proximosDiasHabiles.Last();

            var tpQuery = _context.TareasProyecto.Include(t => t.UsuarioAsignado).Include(t => t.SubFase.Fase).Where(t => t.Estado != "Terminada");
            var spQuery = _context.SubFasesProyecto.Include(s => s.Responsable).Include(s => s.Fase).Where(s => s.Estado != "Terminada");
            if (proyectoId.HasValue) { tpQuery = tpQuery.Where(t => t.SubFase.Fase.ProyectoId == proyectoId.Value); spQuery = spQuery.Where(s => s.Fase.ProyectoId == proyectoId.Value); }

            var tareasPendientes = await tpQuery.ToListAsync();
            var subfasesPendientes = await spQuery.ToListAsync();

            var workItemsChart = new List<WorkItem>();
            foreach (var t in tareasPendientes) { DateTime s = t.FechaInicioReal ?? t.FechaInicioPlanificada ?? t.FechaCreacion; decimal h = t.HorasEstimadas > 0 ? t.HorasEstimadas : 1m; DateTime e = t.FechaFinPlanificada ?? t.FechaFinReal ?? s.AddHours((double)h); workItemsChart.Add(new WorkItem { Email = t.UsuarioAsignado?.Email ?? "Sin Asignar", Start = s, End = e, Horas = h }); }
            foreach (var sub in subfasesPendientes) { if (!sub.FechaInicio.HasValue) continue; DateTime s = sub.FechaInicio.Value; decimal h = sub.HorasEstimadas > 0 ? sub.HorasEstimadas : 1m; DateTime e = sub.FechaFinEstimada ?? s.AddHours((double)h); workItemsChart.Add(new WorkItem { Email = sub.Responsable?.Email ?? "Sin Asignar", Start = s, End = e, Horas = h }); }

            var seriesCargaFuturo = new List<object>();
            if (workItemsChart.Any())
            {
                var usuariosFuturo = workItemsChart.Select(w => w.Email).Distinct().ToList();
                foreach (var userEmail in usuariosFuturo)
                {
                    var dataUsuarioFuturo = new decimal[14];
                    var itemsDelUsuario = workItemsChart.Where(w => w.Email == userEmail).ToList();
                    foreach (var item in itemsDelUsuario)
                    {
                        DateTime startTask = item.Start.Date < DateTime.Today ? DateTime.Today : item.Start.Date;
                        DateTime endTask = item.End.Date < DateTime.Today ? DateTime.Today : item.End.Date;
                        if (endTask < startTask) endTask = startTask;
                        if (startTask > maxFutureDate) continue;
                        int taskWorkingDays = 0;
                        for (var d = startTask; d <= endTask; d = d.AddDays(1)) { if (EsHabil(d, feriadosDb)) taskWorkingDays++; }
                        if (taskWorkingDays == 0) taskWorkingDays = 1;
                        decimal hoursPerDay = item.Horas / taskWorkingDays;
                        for (int i = 0; i < 14; i++) { var diaHabil = proximosDiasHabiles[i]; if (diaHabil >= startTask && diaHabil <= endTask) { dataUsuarioFuturo[i] += hoursPerDay; } }
                    }
                    if (dataUsuarioFuturo.Any(h => h > 0)) seriesCargaFuturo.Add(new { name = userEmail == "Sin Asignar" ? "Sin Asignar" : userEmail.Split('@')[0], data = dataUsuarioFuturo.Select(d => Math.Round(d, 1)).ToList() });
                }
            }
            ViewBag.EtiquetasDiasFuturo = System.Text.Json.JsonSerializer.Serialize(etiquetasDiasFuturo);
            ViewBag.SeriesCargaFuturo = System.Text.Json.JsonSerializer.Serialize(seriesCargaFuturo);

            // ==========================================
            // 4. LISTA DE ACCIÓN (TAREAS ESTANCADAS)
            // ==========================================
            var estancadasQuery = _context.TareasProyecto.Include(t => t.SubFase).ThenInclude(s => s.Fase).ThenInclude(f => f.Proyecto).Include(t => t.UsuarioAsignado).Where(t => t.Estado == "En Progreso" && t.FechaInicioReal.HasValue);
            if (proyectoId.HasValue) estancadasQuery = estancadasQuery.Where(t => t.SubFase.Fase.ProyectoId == proyectoId.Value);
            var tareasEstancadas = await estancadasQuery.OrderBy(t => t.FechaInicioReal).Take(6).AsNoTracking().ToListAsync();
            ViewBag.TareasEstancadas = tareasEstancadas;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetSobrecargaPorSemana(int offset = 0, int? proyectoId = null)
        {
            var resultado = await CalcularSobrecarga(offset, proyectoId);
            return Json(resultado);
        }

        [HttpGet]
        public async Task<IActionResult> GetProyectoDetalle(string nombre)
        {
            var proyecto = await _context.Proyectos
                .Include(p => p.Fases).ThenInclude(f => f.SubFases).ThenInclude(s => s.Tareas).ThenInclude(t => t.UsuarioAsignado)
                .FirstOrDefaultAsync(p => p.Nombre == nombre);

            if (proyecto == null) return NotFound();

            var tareasMalas = proyecto.Fases?.SelectMany(f => f.SubFases ?? Enumerable.Empty<NotebookValidator.Web.Models.GestorProyectos.SubFaseProyecto>())
                .SelectMany(s => s.Tareas ?? Enumerable.Empty<NotebookValidator.Web.Models.GestorProyectos.TareaProyecto>())
                .Where(t => t.Estado == "Terminada" && t.HorasRealesDeducidas > t.HorasEstimadas)
                .Select(t => new {
                    nombre = t.Nombre,
                    responsable = t.UsuarioAsignado?.Email?.Split('@')[0] ?? "Sin Asignar",
                    estimado = t.HorasEstimadas,
                    real = t.HorasRealesDeducidas,
                    exceso = t.HorasRealesDeducidas - t.HorasEstimadas
                }).OrderByDescending(t => t.exceso).ToList();

            return Json(new { proyecto = proyecto.Nombre, tareas = tareasMalas });
        }

        // 🎯 ACTUALIZACIÓN: Recibe el Mensaje del Modal
        [HttpPost]
        public IActionResult NudgeUser([FromBody] NudgeRequest req)
        {
            // Aquí puedes conectar en el futuro con: _notifService.NotificarNudgeAsync(req.TareaId, req.Mensaje);
            return Json(new { success = true, message = "Recordatorio enviado con éxito al responsable." });
        }

        public class NudgeRequest
        {
            public int TareaId { get; set; }
            public string Mensaje { get; set; } = string.Empty;
        }

        private async Task<SobrecargaResult> CalcularSobrecarga(int offsetSemanas, int? proyectoId = null)
        {
            DateTime fechaBase = DateTime.Today.AddDays(offsetSemanas * 7);
            int diff = (7 + (fechaBase.DayOfWeek - DayOfWeek.Monday)) % 7;
            DateTime inicioSemana = fechaBase.AddDays(-1 * diff).Date;
            DateTime finSemana = inicioSemana.AddDays(6).AddHours(23).AddMinutes(59).AddSeconds(59);

            var tq = _context.TareasProyecto.Include(t => t.SubFase.Fase).Where(t => t.UsuarioAsignadoId != null && t.Estado != "Terminada");
            var sq = _context.SubFasesProyecto.Include(s => s.Responsable).Include(s => s.Fase).Where(s => s.ResponsableId != null && s.Estado != "Terminada");

            if (proyectoId.HasValue) { tq = tq.Where(t => t.SubFase.Fase.ProyectoId == proyectoId.Value); sq = sq.Where(s => s.Fase.ProyectoId == proyectoId.Value); }

            var tareasPendientes = await tq.ToListAsync();
            var subfasesPendientes = await sq.ToListAsync();

            var workItems = new List<WorkItem>();

            foreach (var t in tareasPendientes)
            {
                DateTime s = t.FechaInicioReal ?? t.FechaInicioPlanificada ?? t.FechaCreacion;
                var emailStr = _context.Users.Where(u => u.Id == t.UsuarioAsignadoId).Select(u => u.Email).FirstOrDefault() ?? t.UsuarioAsignadoId;
                workItems.Add(new WorkItem { Email = emailStr, Start = s, Horas = t.HorasEstimadas });
            }
            foreach (var sub in subfasesPendientes)
            {
                if (sub.FechaInicio.HasValue)
                    workItems.Add(new WorkItem { Email = sub.Responsable?.Email ?? sub.ResponsableId, Start = sub.FechaInicio.Value, Horas = sub.HorasEstimadas });
            }

            var itemsEnRango = workItems.Where(w => w.Start >= inicioSemana && w.Start <= finSemana);

            var usuariosSobrecargados = itemsEnRango
                .GroupBy(w => w.Email)
                .Select(g => new { Email = g.Key, TotalHoras = g.Sum(x => x.Horas) })
                .Where(x => x.TotalHoras > 40)
                .OrderByDescending(x => x.TotalHoras)
                .Select(x => new SobrecargaUsuario { Email = x.Email ?? "Sin Asignar", Nombre = (x.Email ?? "Sin Asignar").Split('@')[0], Horas = Math.Round(x.TotalHoras, 1) })
                .ToList();

            string label = offsetSemanas == 0 ? "ESTA SEMANA" : (offsetSemanas == 1 ? "PRÓX. SEMANA" : (offsetSemanas == -1 ? "SEM. PASADA" : $"SEMANA {offsetSemanas}"));
            string rango = $"{inicioSemana:dd MMM} - {finSemana:dd MMM}";

            return new SobrecargaResult { Count = usuariosSobrecargados.Count, Label = label, Rango = rango, Usuarios = usuariosSobrecargados };
        }

        private class SobrecargaUsuario { public string Email { get; set; } = string.Empty; public string Nombre { get; set; } = string.Empty; public decimal Horas { get; set; } }
        private class SobrecargaResult { public int Count { get; set; } public string Label { get; set; } = string.Empty; public string Rango { get; set; } = string.Empty; public List<SobrecargaUsuario> Usuarios { get; set; } = new List<SobrecargaUsuario>(); }
    }
}
