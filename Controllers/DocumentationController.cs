using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NotebookValidator.Web.Services;
using NotebookValidator.Web.ViewModels;
using System;
using System.IO;
using System.Threading.Tasks;

namespace NotebookValidator.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DocumentationController : Controller
    {
        private readonly DocumentationService _documentationService;
        private readonly IWebHostEnvironment _hostEnvironment;

        public DocumentationController(DocumentationService documentationService, IWebHostEnvironment hostEnvironment)
        {
            _documentationService = documentationService;
            _hostEnvironment = hostEnvironment;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new DocumentationViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GeneratePreview(DocumentationViewModel model)
        {
            if (model.NotebookFile == null || !model.NotebookFile.FileName.EndsWith(".ipynb"))
            {
                TempData["ToastMessage"] = "Por favor, selecciona un archivo .ipynb válido.";
                TempData["ToastType"] = "error";
                return View("Index", new DocumentationViewModel());
            }
            try
            {
                string generatedMarkdown = await _documentationService.GetDocumentationMarkdownAsync(model.NotebookFile);
                
                var resultModel = new DocumentationViewModel
                {
                    GeneratedMarkdown = generatedMarkdown,
                    OriginalFileName = Path.GetFileNameWithoutExtension(model.NotebookFile.FileName)
                };
                
                TempData["ToastMessage"] = "Vista previa generada correctamente.";
                TempData["ToastType"] = "success";
                
                TempData["GeneratedMarkdown"] = generatedMarkdown;
                TempData["OriginalFileName"] = resultModel.OriginalFileName;
                
                return View("Index", resultModel);
            }
            catch (Exception ex)
            {
                TempData["ToastMessage"] = $"Ocurrió un error: {ex.Message}";
                TempData["ToastType"] = "error";
                return View("Index", new DocumentationViewModel { GeneratedMarkdown = $"Error: {ex.Message}" });
            }
        }

        [HttpGet]
        public IActionResult DownloadAsDocx()
        {
            string markdownToExport = TempData["GeneratedMarkdown"] as string;
            string originalFileName = TempData["OriginalFileName"] as string ?? "documento";

            if (string.IsNullOrEmpty(markdownToExport))
            {
                TempData["ToastMessage"] = "No hay documentación generada para exportar. Por favor, genera una vista previa primero.";
                TempData["ToastType"] = "error";
                return RedirectToAction("Index");
            }
            
            TempData.Keep("GeneratedMarkdown");
            TempData.Keep("OriginalFileName");

            try
            {
                var templatePath = Path.Combine(_hostEnvironment.WebRootPath, "templates", "01-Doc_Matriz_Excesos_Hipotecario.docx");
                byte[] generatedDocument = _documentationService.CreateWordDocument(markdownToExport, templatePath, originalFileName);
                var outputFileName = $"{originalFileName}_documentacion.docx";
                return File(generatedDocument, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", outputFileName);
            }
            catch (Exception ex)
            {
                TempData["ToastMessage"] = $"Ocurrió un error al crear el archivo .docx: {ex.Message}";
                TempData["ToastType"] = "error";
                return RedirectToAction("Index");
            }
        }
    }
}