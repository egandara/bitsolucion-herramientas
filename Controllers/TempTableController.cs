using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NotebookValidator.Web.Services;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace NotebookValidator.Web.Controllers
{
    [Authorize(Roles = "Admin, TempCleanerUser")]
    public class TempTableController : Controller
    {
        private readonly TempTableService _tempTableService;

        public TempTableController(TempTableService tempTableService)
        {
            _tempTableService = tempTableService;
        }

        // Muestra la página de carga de archivos
        public IActionResult Index()
        {
            return View();
        }

        // Procesa los archivos y genera el notebook de salida
        [HttpPost]
        public async Task<IActionResult> Generate(IFormFileCollection files)
        {
            if (files == null || files.Count == 0)
            {
                TempData["ToastMessage"] = "Por favor, selecciona al menos un archivo para analizar.";
                TempData["ToastType"] = "error";
                return RedirectToAction("Index");
            }

            try
            {
                // Llama al servicio para generar el notebook
                var generatedNotebookJson = await _tempTableService.GenerateDeletionNotebookAsync(files);
                var fileName = $"BCI_05_eliminacion_tablas_temporales_generado_{DateTime.Now:yyyyMMdd}.ipynb";
                
                // Devuelve el archivo generado para su descarga
                return File(Encoding.UTF8.GetBytes(generatedNotebookJson), "application/vnd.jupyter", fileName);
            }
            catch (Exception ex)
            {
                TempData["ToastMessage"] = $"Ocurrió un error: {ex.Message}";
                TempData["ToastType"] = "error";
                return RedirectToAction("Index");
            }
        }
    }
}