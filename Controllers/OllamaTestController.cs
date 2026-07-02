using Google.Apis.Drive.v3.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NotebookValidator.Web.Services;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;

namespace NotebookValidator.Web.Controllers
{
    public class OllamaTestController : Controller
    {
        private readonly OllamaBackgroundManager _backgroundManager;

        public OllamaTestController(OllamaBackgroundManager backgroundManager)
        {
            _backgroundManager = backgroundManager;
        }

        // --- SISTEMA DE IDENTIFICACIÓN ---
        private string ObtenerUsuarioActual()
        {
            // Usamos la sesión oficial de .NET. Si no está logueado, usamos su SessionId
            if (User?.Identity?.IsAuthenticated == true && !string.IsNullOrEmpty(User.Identity.Name))
            {
                return User.Identity.Name;
            }
            return HttpContext.Session.Id ?? "Usuario_Desconocido";
        }

        [HttpGet]
        public IActionResult Index(string? jobId)
        {
            // Si la URL viene con un parámetro ?jobId=... (desde la notificación)
            if (!string.IsNullOrEmpty(jobId))
            {
                string usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "usuario_desconocido";

                // Validamos que el Job exista y pertenezca al usuario que está logueado
                var job = _backgroundManager.ObtenerEstadoSeguro(jobId, usuarioId);

                if (job != null)
                {
                    // Le pasamos el JobId a la Vista para activar el bloque de visualización
                    ViewBag.JobId = jobId;

                    // Opcional: Si el job ya terminó, podemos inyectar de inmediato los prompts en el formulario por estética
                    ViewBag.Prompt = "Análisis cargado desde historial.";
                    ViewBag.SistemaPrompt = "Enfócate en la lógica de extracción, las tablas afectadas y la cuadratura de datos.";
                }
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(string prompt, string sistemaPrompt, string modeloIA, List<IFormFile> archivos)
        {
            if (string.IsNullOrWhiteSpace(prompt) && (archivos == null || archivos.Count == 0))
            {
                ViewBag.Error = "Debes ingresar una instrucción o adjuntar al menos un archivo.";
                return View();
            }

            ViewBag.Prompt = prompt;
            ViewBag.SistemaPrompt = sistemaPrompt;

            var archivosContenido = new Dictionary<string, string>();
            if (archivos != null && archivos.Count > 0)
            {
                foreach (var archivo in archivos)
                {
                    if (archivo.Length > 0)
                    {
                        using (var reader = new StreamReader(archivo.OpenReadStream()))
                        {
                            string contenido = await reader.ReadToEndAsync();
                            archivosContenido.Add(archivo.FileName, contenido);
                        }
                    }
                }
            }

            string instruccionLimpia = string.IsNullOrWhiteSpace(prompt)
                ? "Analiza detalladamente el archivo adjunto y genera su documentación técnica." : prompt;

            string usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "usuario_desconocido";

            // ENVIAMOS EL MODELO IA AL GESTOR DE FONDO
            string jobId = _backgroundManager.IniciarProceso(instruccionLimpia, sistemaPrompt, usuarioId, archivosContenido, modeloIA);

            ViewBag.JobId = jobId;

            return View();
        }

        // Endpoint de consulta asíncrona
        [HttpGet]
        public IActionResult CheckStatus(string jobId)
        {
            // 1. Forzamos a obtener el GUID exacto del usuario (el mismo usado al iniciar el proceso)
            string usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "usuario_desconocido";

            // 2. Consultamos al manager de forma segura
            var job = _backgroundManager.ObtenerEstadoSeguro(jobId, usuarioId);

            // 3. SOLUCIÓN AL ERROR 'A': Si no se encuentra o no hay acceso, devolvemos JSON estructurado
            // en lugar de un texto plano "Acceso Denegado", así el JavaScript no se rompe.
            if (job == null)
            {
                return Json(new
                {
                    terminado = true,
                    estado = "Error de autenticación",
                    resultado = "No se encontró el ticket de IA o no tienes permisos para verlo.",
                    huboError = true
                });
            }

            // 4. Si todo está correcto, devolvemos el objeto en formato JSON
            return Json(job);
        }

        // --- ENDPOINTS DE HISTORIAL Y NOTIFICACIONES ---

        [HttpGet]
        public IActionResult Historial()
        {
            var historial = _backgroundManager.ObtenerHistorialUsuario(ObtenerUsuarioActual());
            return View(historial);
        }

        [HttpGet]
        public IActionResult VerResultado(string id)
        {
            var job = _backgroundManager.ObtenerEstadoSeguro(id, ObtenerUsuarioActual());
            if (job == null) return NotFound("No tienes permiso para ver este documento o ya expiró.");

            return View(job);
        }

        [HttpGet]
        public IActionResult PingNotificaciones()
        {
            int pendientes = _backgroundManager.ContarTareasCompletadasNoNotificadas(ObtenerUsuarioActual());
            return Json(new { count = pendientes });
        }

        [HttpPost]
        public IActionResult LimpiarNotificaciones()
        {
            _backgroundManager.MarcarNotificacionesLeidas(ObtenerUsuarioActual());
            return Ok();
        }
    }
}
