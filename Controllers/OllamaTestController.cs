using Google.Apis.Drive.v3.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NotebookValidator.Web.Models;
using NotebookValidator.Web.Services;
using System.Collections.Generic;
using System.IO;
using System.Linq; // Necesario para Linq
using System.Security.Claims;
using System.Threading.Tasks;
using NotebookValidator.Web.Data;

namespace NotebookValidator.Web.Controllers
{
    // DTO para recibir el historial de chat desde JavaScript
    public class ChatRequestDto
    {
        public List<ChatMessage> Historial { get; set; } = new();
        public string ModeloIA { get; set; } = string.Empty;
        public string SistemaPrompt { get; set; } = string.Empty;
    }

    public class OllamaTestController : Controller
    {
        private readonly OllamaBackgroundManager _backgroundManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly OllamaTestService _ollamaService; // INYECTAMOS EL SERVICIO DIRECTO PARA EL CHAT

        public OllamaTestController(OllamaBackgroundManager backgroundManager, UserManager<ApplicationUser> userManager, OllamaTestService ollamaService)
        {
            _backgroundManager = backgroundManager;
            _userManager = userManager;
            _ollamaService = ollamaService;
        }

        private string ObtenerUsuarioActual()
        {
            if (User?.Identity?.IsAuthenticated == true && !string.IsNullOrEmpty(User.Identity.Name))
                return User.Identity.Name;
            return HttpContext.Session.Id ?? "Usuario_Desconocido";
        }

        [HttpGet]
        public IActionResult Index(string? jobId)
        {
            if (!string.IsNullOrEmpty(jobId))
            {
                string usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "usuario_desconocido";
                var job = _backgroundManager.ObtenerEstadoSeguro(jobId, usuarioId);

                if (job != null)
                {
                    ViewBag.JobId = jobId;
                    ViewBag.Prompt = "Análisis cargado desde historial.";
                    ViewBag.SistemaPrompt = "Enfócate en la lógica de extracción, las tablas afectadas y la cuadratura de datos.";
                }
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(string prompt, string sistemaPrompt, string modeloIA, List<IFormFile> archivos)
        {
            // Este método se mantiene IGUAL para la "Documentación" pesada
            if (string.IsNullOrWhiteSpace(prompt) && (archivos == null || archivos.Count == 0))
            {
                ViewBag.Error = "Debes ingresar una instrucción o adjuntar al menos un archivo.";
                return View();
            }

            int costoBase = 0;
            int costoPorArchivo = 0;
            int cantidadArchivos = archivos?.Count ?? 0;

            switch (modeloIA)
            {
                case "phi3:latest": costoBase = 2; costoPorArchivo = 1; break;
                case "qwen2.5:32b": costoBase = 25; costoPorArchivo = 5; break;
                case "qwen2.5-coder:7b":
                case "qwen2.5:7b":
                default: costoBase = 5; costoPorArchivo = 3; break;
            }

            int costoTotal = costoBase + (costoPorArchivo * cantidadArchivos);

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                ViewBag.Error = "Debes iniciar sesión de forma segura.";
                return View();
            }

            if (user.AnalysisQuota < costoTotal)
            {
                ViewBag.Error = $"Créditos insuficientes. Requieres {costoTotal} créditos (Saldo actual: {user.AnalysisQuota} ⚡).";
                ViewBag.Prompt = prompt;
                ViewBag.SistemaPrompt = sistemaPrompt;
                return View();
            }

            user.AnalysisQuota -= costoTotal;
            await _userManager.UpdateAsync(user);

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

            string instruccionLimpia = string.IsNullOrWhiteSpace(prompt) ? "Analiza detalladamente el archivo adjunto." : prompt;
            string jobId = _backgroundManager.IniciarProceso(instruccionLimpia, sistemaPrompt, user.Id, archivosContenido, modeloIA);
            ViewBag.JobId = jobId;

            return View();
        }

        // =========================================================================
        // NUEVO ENDPOINT: CHAT MULTI-TURNO EN TIEMPO REAL (AJAX)
        // =========================================================================
        [HttpPost]
        public async Task<IActionResult> EnviarMensajeChat([FromBody] ChatRequestDto request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false, message = "Sesión inválida." });

            // 1. TARIFA POR MENSAJE DE CHAT
            int costoPorMensaje = 0;
            switch (request.ModeloIA)
            {
                case "phi3:latest": costoPorMensaje = 1; break;
                case "qwen2.5:32b": costoPorMensaje = 10; break;
                case "qwen2.5-coder:7b":
                case "qwen2.5:7b":
                default: costoPorMensaje = 3; break;
            }

            // 2. VALIDAR SALDO
            if (user.AnalysisQuota < costoPorMensaje)
            {
                return Json(new { success = false, message = $"Créditos insuficientes. Necesitas {costoPorMensaje} ⚡ para enviar un mensaje con este modelo." });
            }

            // 3. COBRAR
            user.AnalysisQuota -= costoPorMensaje;
            await _userManager.UpdateAsync(user);

            // 4. CONSULTAR A LA IA
            string respuesta = await _ollamaService.ConversarAsync(request.Historial, request.SistemaPrompt, request.ModeloIA);

            return Json(new { success = true, respuesta = respuesta, creditosRestantes = user.AnalysisQuota });
        }

        // ... (El resto de tus métodos CheckStatus, Historial, VerResultado, PingNotificaciones se mantienen igual) ...
        [HttpGet]
        public IActionResult CheckStatus(string jobId)
        {
            string usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "usuario_desconocido";
            var job = _backgroundManager.ObtenerEstadoSeguro(jobId, usuarioId);

            if (job == null) return Json(new { terminado = true, estado = "Error de autenticación", resultado = "Ticket no encontrado.", huboError = true });
            return Json(job);
        }

        [HttpGet] public IActionResult Historial() => View(_backgroundManager.ObtenerHistorialUsuario(ObtenerUsuarioActual()));
        [HttpGet] public IActionResult VerResultado(string id) { var job = _backgroundManager.ObtenerEstadoSeguro(id, ObtenerUsuarioActual()); if (job == null) return NotFound("Expirado."); return View(job); }
        [HttpGet] public IActionResult PingNotificaciones() => Json(new { count = _backgroundManager.ContarTareasCompletadasNoNotificadas(ObtenerUsuarioActual()) });
        [HttpPost] public IActionResult LimpiarNotificaciones() { _backgroundManager.MarcarNotificacionesLeidas(ObtenerUsuarioActual()); return Ok(); }
    }
}
