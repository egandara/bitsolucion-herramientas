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
                url = n.Url ?? "#",
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
    }
}
