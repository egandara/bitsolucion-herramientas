using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotebookValidator.Web.Services;
using System.Security.Claims;

namespace NotebookValidator.Web.Controllers
{
    [Authorize]
    public class NotificacionesController : Controller
    {
        private readonly NotificacionesService _notifService;

        public NotificacionesController(NotificacionesService notifService)
        {
            _notifService = notifService;
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerNoLeidas()
        {
            string uid = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            var notifs = await _notifService.ObtenerNoLeidasAsync(uid);
            return Json(notifs.Select(n => new
            {
                id = n.Id,
                tipo = n.Tipo,
                titulo = n.Titulo,
                descripcion = n.Descripcion,
                // CAMBIO AQUÍ: Generamos la ruta hacia nuestra "ruta puente"
                url = Url.Action("RedirigirYMarcarLeida", "Notificaciones", new { id = n.Id }) ?? "#",
                fecha = n.FechaCreacion.ToLocalTime().ToString("dd/MM HH:mm")
            }));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarcarTodasLeidas()
        {
            string uid = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            await _notifService.MarcarTodasLeidasAsync(uid);
            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> RedirigirYMarcarLeida(int id)
        {
            string uid = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

            // Marcar como leída y obtener la URL de destino
            string? urlDestino = await _notifService.LeerYObtenerUrlAsync(id, uid);

            // Si no se encuentra o no tiene URL, redirigir al inicio como fallback
            if (string.IsNullOrEmpty(urlDestino))
            {
                return RedirectToAction("Index", "Home");
            }

            return Redirect(urlDestino);
        }
    }
}
