using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using NotebookValidator.Web.Services;
using System;
using System.IO;
using System.Threading.Tasks;

namespace NotebookValidator.Web.Controllers
{
    public class OllamaTestController : Controller
    {
        private readonly OllamaTestService _ollamaService;

        public OllamaTestController(OllamaTestService ollamaService)
        {
            _ollamaService = ollamaService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(string prompt, string sistemaPrompt, IFormFile? archivo)
        {
            if (string.IsNullOrWhiteSpace(prompt) && archivo == null)
            {
                ViewBag.Error = "Debes ingresar una instrucción o adjuntar un archivo para analizar.";
                return View();
            }

            ViewBag.Prompt = prompt;
            ViewBag.SistemaPrompt = sistemaPrompt;

            string promptFinal = "";

            // 1. Inyectamos primero el archivo como Contexto Operacional
            if (archivo != null && archivo.Length > 0)
            {
                using (var reader = new StreamReader(archivo.OpenReadStream()))
                {
                    string contenidoArchivo = await reader.ReadToEndAsync();

                    promptFinal += $"--- INICIO DEL CÓDIGO FUENTE DEL ARCHIVO ({archivo.FileName}) ---\n";
                    promptFinal += contenidoArchivo;
                    promptFinal += $"\n--- FIN DEL CÓDIGO FUENTE DEL ARCHIVO ---\n\n";
                }
                ViewBag.ArchivoNombre = archivo.FileName;
            }

            // 2. Colocamos la instrucción al final para mitigar el efecto autocompletado
            string instruccionLimpia = string.IsNullOrWhiteSpace(prompt)
                ? "Analiza detalladamente el archivo adjunto y genera su documentación técnica estructurada."
                : prompt;

            promptFinal += $"INSTRUCCIÓN DEL USUARIO: {instruccionLimpia}";

            // 3. Ejecutamos la consulta pasándola por el filtro fijo corporativo
            var respuestaIA = await _ollamaService.GenerarTextoAsync(promptFinal, sistemaPrompt);

            ViewBag.Respuesta = respuestaIA;

            return View();
        }
    }
}
