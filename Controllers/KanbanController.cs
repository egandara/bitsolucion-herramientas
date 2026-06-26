using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NotebookValidator.Web.Data;
using NotebookValidator.Web.Services;
using System.Security.Claims;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace NotebookValidator.Web.Controllers
{
    [Authorize]
    public class KanbanController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly NotificacionesService _notifService;

        public KanbanController(ApplicationDbContext context, NotificacionesService notifService)
        {
            _context = context;
            _notifService = notifService;
        }

        public async Task<IActionResult> Index(int? proyectoId = null)
        {
            string currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

            var proyectosDisponibles = await _context.Proyectos
                .Where(p => p.Estado == "Activo" || p.Estado == "En Desarrollo")
                .OrderBy(p => p.Nombre)
                .ToListAsync();

            ViewBag.FiltroProyectos = new SelectList(proyectosDisponibles, "Id", "Nombre", proyectoId);
            ViewBag.ProyectoActivo = proyectoId;

            var query = _context.TareasProyecto
                .Include(t => t.SubFase).ThenInclude(s => s.Fase).ThenInclude(f => f.Proyecto)
                .Include(t => t.UsuarioAsignado)
                .Where(t => t.SubFase.Fase.Proyecto.Estado != "Eliminado")
                .AsQueryable();

            if (proyectoId.HasValue) query = query.Where(t => t.SubFase.Fase.ProyectoId == proyectoId.Value);
            else query = query.Where(t => t.UsuarioAsignadoId == currentUserId);

            var tareas = await query.ToListAsync();

            return View(tareas);
        }

        // ==========================================
        // SISTEMA DE DETALLES, DESCRIPCIONES Y CHAT
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> GetDetalleTarea(int tareaId)
        {
            var tarea = await _context.TareasProyecto
                .Include(t => t.UsuarioAsignado)
                .Include(t => t.SubFase).ThenInclude(s => s.Fase).ThenInclude(f => f.Proyecto)
                .FirstOrDefaultAsync(t => t.Id == tareaId);

            if (tarea == null) return NotFound();

            var comentarios = new List<dynamic>();

            try
            {
                var listaDb = await _context.Set<NotebookValidator.Web.Models.GestorProyectos.ComentarioTarea>()
                    .Where(c => c.TareaProyectoId == tareaId)
                    .OrderBy(c => c.FechaCreacion)
                    .ToListAsync();

                comentarios = listaDb.Select(c => (dynamic)new
                {
                    usuario = c.UsuarioAlias,
                    texto = c.Texto,
                    fechaStr = c.FechaCreacion.ToString("dd MMM, HH:mm")
                }).ToList();
            }
            catch
            {
                // Si entra aquí, es por seguridad en caso de desincronización
            }

            return Json(new
            {
                id = tarea.Id,
                nombre = tarea.Nombre,
                descripcion = tarea.Descripcion ?? "",
                estado = tarea.Estado,
                horas = tarea.HorasEstimadas,
                responsable = tarea.UsuarioAsignado?.Email?.Split('@')[0] ?? "Sin Asignar",
                proyectoNombre = tarea.SubFase?.Fase?.Proyecto?.Nombre ?? "Global",
                faseNombre = tarea.SubFase?.Fase?.NombreFase?.Replace("_", " ") ?? "",
                subFaseNombre = tarea.SubFase?.Nombre ?? "",
                fechaInicio = tarea.FechaInicioPlanificada?.ToString("yyyy-MM-dd"),
                fechaFin = tarea.FechaFinPlanificada?.ToString("yyyy-MM-dd"),
                comentarios = comentarios
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTaskDescription([FromBody] UpdateDescriptionRequest req)
        {
            var tarea = await _context.TareasProyecto.FindAsync(req.TareaId);
            if (tarea == null) return NotFound();

            tarea.Descripcion = req.Descripcion?.Trim();
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComentario([FromBody] NuevoComentarioTarea request)
        {
            if (string.IsNullOrWhiteSpace(request.Texto)) return Json(new { success = false, message = "El texto está vacío." });

            string alias = (User.Identity?.Name ?? "Anónimo").Split('@')[0];

            try
            {
                var comentario = new NotebookValidator.Web.Models.GestorProyectos.ComentarioTarea
                {
                    TareaProyectoId = request.TareaId,
                    UsuarioAlias = alias,
                    Texto = request.Texto.Trim(),
                    FechaCreacion = DateTime.Now
                };

                _context.Add(comentario);

                var menciones = Regex.Matches(request.Texto, @"@([\w\.]+)")
                                     .Select(m => m.Groups[1].Value)
                                     .Distinct()
                                     .ToList();

                if (menciones.Any())
                {
                    var tarea = await _context.TareasProyecto
                        .Include(t => t.SubFase.Fase.Proyecto)
                        .FirstOrDefaultAsync(t => t.Id == request.TareaId);

                    if (tarea?.SubFase?.Fase?.Proyecto != null)
                    {
                        foreach (var mencionado in menciones)
                        {
                            await _notifService.NotificarMencionAsync(tarea.SubFase.Fase.Proyecto, mencionado, alias);
                        }
                    }
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, usuario = alias, texto = comentario.Texto, fechaStr = comentario.FechaCreacion.ToString("dd MMM, HH:mm") });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error BD. " + ex.Message });
            }
        }

        // ==========================================
        // SISTEMA DE REASIGNACIÓN
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var usersDb = await _context.Users
                    .Select(u => new { u.Id, u.Email })
                    .ToListAsync();

                var users = usersDb
                    .Select(u => new {
                        id = u.Id,
                        alias = u.Email != null ? u.Email.Split('@')[0] : "Desconocido"
                    })
                    .OrderBy(u => u.alias)
                    .ToList();

                return Json(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTaskAssignee([FromBody] ReassignRequest req)
        {
            var tarea = await _context.TareasProyecto.FindAsync(req.TareaId);
            if (tarea == null) return NotFound();

            tarea.UsuarioAsignadoId = req.UserId;
            await _context.SaveChangesAsync();

            var newAlias = _context.Users.Find(req.UserId)?.Email?.Split('@')[0] ?? "Sin Asignar";

            return Json(new { success = true, newAlias = newAlias });
        }

        public class NuevoComentarioTarea { public int TareaId { get; set; } public string Texto { get; set; } = string.Empty; }
        public class ReassignRequest { public int TareaId { get; set; } public string UserId { get; set; } = string.Empty; }
        public class UpdateDescriptionRequest { public int TareaId { get; set; } public string? Descripcion { get; set; } }
    }
}
