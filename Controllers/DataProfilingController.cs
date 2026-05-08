using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NotebookValidator.Web.Services;
using System;
using System.Linq;

namespace NotebookValidator.Web.Controllers
{
    [Authorize]
    public class DataProfilingController : Controller
    {
        private readonly DataProfilingService _profilingService;

        public DataProfilingController(DataProfilingService profilingService)
        {
            _profilingService = profilingService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Analyze(IFormFile archivo, bool tieneEncabezados = true)
        {
            if (archivo == null || archivo.Length == 0)
            {
                TempData["ToastMessage"] = "Por favor, selecciona un archivo válido.";
                TempData["ToastType"] = "error";
                return RedirectToAction("Index");
            }

            try
            {
                var profileResult = _profilingService.AnalyzeFile(archivo, tieneEncabezados);
                return View("Resultados", profileResult);
            }
            catch (Exception ex)
            {
                TempData["ToastMessage"] = $"Error al analizar el archivo: {ex.Message}";
                TempData["ToastType"] = "error";
                return RedirectToAction("Index");
            }
        }
    }
}
