using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NotebookValidator.Web.Services;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NotebookValidator.Web.Controllers
{
    public class DocGenController : Controller
    {
        private readonly DocGenService _docGenService;

        public DocGenController(DocGenService docGenService)
        {
            _docGenService = docGenService;
        }

        [HttpGet]
        public IActionResult Index() => View();

        [HttpPost]
        public async Task<IActionResult> ProcessNotebooks(List<IFormFile> files)
        {
            if (files == null || !files.Any())
            {
                return Json(new { success = false, message = "Por favor, selecciona al menos un archivo válido (.ipynb o .py)." });
            }

            var results = new List<object>();

            foreach (var file in files)
            {
                string ext = Path.GetExtension(file.FileName).ToLower();
                if (ext != ".ipynb" && ext != ".py") continue;

                // Recibe el par estructurado de datos (Markdown para visor, HTML estructurado para Word)
                var (markdownDoc, htmlDoc) = await _docGenService.GenerateDocumentationPairAsync(file);

                results.Add(new
                {
                    fileName = file.FileName,
                    cleanName = Path.GetFileNameWithoutExtension(file.FileName),
                    markdown = markdownDoc,
                    html = htmlDoc
                });
            }

            return Json(new { success = true, documents = results });
        }

        // NUEVO ENDPOINT: Recibe el HTML formateado del cliente y lo descarga como Word Nativo editable
        [HttpPost]
        public IActionResult DownloadWordPackage(string fileName, string htmlContent)
        {
            if (string.IsNullOrEmpty(htmlContent)) return BadRequest("Contenido de la ficha técnica vacío.");

            byte[] fileBytes = System.Text.Encoding.UTF8.GetBytes(htmlContent);
            string safeFileName = $"Ficha_Tecnica_{fileName.Replace(" ", "_")}.doc";

            // Retorna como documento de Word binario clásico abrible de forma nativa por Office
            return File(fileBytes, "application/vnd.ms-word", safeFileName);
        }
    }
}
