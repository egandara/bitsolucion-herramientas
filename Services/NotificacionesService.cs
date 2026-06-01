using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NotebookValidator.Web.Data;
using NotebookValidator.Web.Hubs;
using NotebookValidator.Web.Models;
using NotebookValidator.Web.Models.GestorProyectos;

namespace NotebookValidator.Web.Services
{
    public class NotificacionesService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NotificacionesHub> _hub;

        public NotificacionesService(
            ApplicationDbContext context,
            IHubContext<NotificacionesHub> hub)
        {
            _context = context;
            _hub = hub;
        }

        // ── Método principal: crear y enviar notificación ─────────
        public async Task EnviarAsync(
            string usuarioId,
            string tipo,
            string titulo,
            string? descripcion = null,
            string? url = null,
            int? proyectoId = null)
        {
            var notif = new NotificacionProyecto
            {
                UsuarioId = usuarioId,
                ProyectoId = proyectoId,
                Tipo = tipo,
                Titulo = titulo,
                Descripcion = descripcion,
                Url = url,
                Leida = false,
                FechaCreacion = DateTime.UtcNow
            };

            _context.Notificaciones.Add(notif);
            await _context.SaveChangesAsync();

            // Enviar en tiempo real al usuario (si está conectado)
            await _hub.Clients
                .Group($"user-{usuarioId}")
                .SendAsync("NuevaNotificacion", new
                {
                    id = notif.Id,
                    tipo = notif.Tipo,
                    titulo = notif.Titulo,
                    descripcion = notif.Descripcion,
                    url = notif.Url,
                    fecha = notif.FechaCreacion.ToLocalTime().ToString("dd/MM HH:mm")
                });
        }

        // ── Helpers por tipo de evento ────────────────────────────

        public async Task NotificarValidacionRechazadaAsync(
            Proyecto proyecto, string usuarioQueValido)
        {
            // Notificar a todos los miembros del proyecto
            var miembros = await _context.ProyectosUsuarios
                .Where(pu => pu.ProyectoId == proyecto.Id)
                .Select(pu => pu.UsuarioId)
                .ToListAsync();

            foreach (var uid in miembros)
                await EnviarAsync(uid, "ValidacionRechazada",
                    $"QA Rechazado — {proyecto.Nombre}",
                    $"El código fue rechazado por el motor de QA. Requiere correcciones.",
                    $"/Proyectos/Details/{proyecto.Id}#calidad",
                    proyecto.Id);
        }

        public async Task NotificarCodigoSubidoAsync(
            Proyecto proyecto, string usuarioQueSubio)
        {
            // Notificar a los demás miembros (no al que subió)
            var miembros = await _context.ProyectosUsuarios
                .Where(pu => pu.ProyectoId == proyecto.Id && pu.UsuarioId != usuarioQueSubio)
                .Select(pu => pu.UsuarioId)
                .ToListAsync();

            var user = await _context.Users.FindAsync(usuarioQueSubio);
            string nombre = user?.Email?.Split('@')[0] ?? "Un usuario";

            foreach (var uid in miembros)
                await EnviarAsync(uid, "CodigoSubido",
                    $"Nuevo código — {proyecto.Nombre}",
                    $"{nombre} actualizó el workspace con código nuevo.",
                    $"/Proyectos/Details/{proyecto.Id}",
                    proyecto.Id);
        }

        public async Task NotificarMencionAsync(
            Proyecto proyecto, string usuarioMencionado, string usuarioQueEscribio)
        {
            // Buscar el userId por username
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email != null &&
                    u.Email.StartsWith(usuarioMencionado + "@"));

            if (user == null) return;

            string autor = usuarioQueEscribio;
            await EnviarAsync(user.Id, "Mencion",
                $"Te mencionaron en {proyecto.Nombre}",
                $"@{usuarioQueEscribio} te mencionó en la bitácora del proyecto.",
                $"/Proyectos/Details/{proyecto.Id}#dashboard",
                proyecto.Id);
        }

        public async Task NotificarDocumentoSubidoAsync(
            Proyecto proyecto, string subcategoria, string usuarioQueSubio)
        {
            var miembros = await _context.ProyectosUsuarios
                .Where(pu => pu.ProyectoId == proyecto.Id && pu.UsuarioId != usuarioQueSubio)
                .Select(pu => pu.UsuarioId)
                .ToListAsync();

            var user = await _context.Users.FindAsync(usuarioQueSubio);
            string nombre = user?.Email?.Split('@')[0] ?? "Un usuario";

            foreach (var uid in miembros)
                await EnviarAsync(uid, "DocumentoSubido",
                    $"Nuevo documento — {proyecto.Nombre}",
                    $"{nombre} subió un documento: {subcategoria}.",
                    $"/Proyectos/Details/{proyecto.Id}#dashboard",
                    proyecto.Id);
        }

        public async Task NotificarFaseCambiadaAsync(
            Proyecto proyecto, string nombreFase, string nuevoEstado, string usuarioQueActualizo)
        {
            var miembros = await _context.ProyectosUsuarios
                .Where(pu => pu.ProyectoId == proyecto.Id && pu.UsuarioId != usuarioQueActualizo)
                .Select(pu => pu.UsuarioId)
                .ToListAsync();

            if (nuevoEstado != "Completado") return; // Solo notificar cuando una fase se completa

            foreach (var uid in miembros)
                await EnviarAsync(uid, "FaseCambiada",
                    $"Fase completada — {proyecto.Nombre}",
                    $"La fase \"{nombreFase.Replace("_", " ")}\" fue marcada como Completada.",
                    $"/Proyectos/Details/{proyecto.Id}#dashboard",
                    proyecto.Id);
        }

        // ── Leer notificaciones de un usuario ─────────────────────
        public async Task<List<NotificacionProyecto>> ObtenerNoLeidasAsync(string usuarioId)
        {
            return await _context.Notificaciones
                .Where(n => n.UsuarioId == usuarioId && !n.Leida)
                .OrderByDescending(n => n.FechaCreacion)
                .Take(20)
                .ToListAsync();
        }

        public async Task MarcarTodasLeidasAsync(string usuarioId)
        {
            var noLeidas = await _context.Notificaciones
                .Where(n => n.UsuarioId == usuarioId && !n.Leida)
                .ToListAsync();

            noLeidas.ForEach(n => n.Leida = true);
            await _context.SaveChangesAsync();
        }
    }
}
