using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using NotebookValidator.Web.Services;
using NotebookValidator.Web.ViewModels;
using System;
using System.IO;

namespace NotebookValidator.Web.Controllers
{
    [Authorize]
    public class GeminiDocController : Controller
    {
        private readonly WordExportService _wordExportService;
        private readonly IWebHostEnvironment _hostEnvironment;

        public GeminiDocController(WordExportService wordExportService, IWebHostEnvironment hostEnvironment)
        {
            _wordExportService = wordExportService;
            _hostEnvironment = hostEnvironment;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ExportToDocx([FromBody] GeminiExportViewModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.MarkdownContent))
            {
                return BadRequest("No se proporcionó contenido para exportar.");
            }

            try
            {
                var templatePath = Path.Combine(_hostEnvironment.WebRootPath, "templates", "01-Doc_Matriz_Excesos_Hipotecario.docx");
                
                if (!System.IO.File.Exists(templatePath))
                {
                     return StatusCode(500, "Error crítico: No se encontró el archivo de plantilla en el servidor.");
                }

                // Llamamos al nuevo método que genera el documento desde cero
                byte[] generatedDocument = _wordExportService.CreateDocumentFromMarkdown(model.MarkdownContent, model.NotebookName, templatePath);
                
                var outputFileName = $"{model.NotebookName}_documentacion_gemini.docx";
                return File(generatedDocument, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", outputFileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ocurrió un error al crear el archivo .docx: {ex.Message}");
            }
        }
    }
}