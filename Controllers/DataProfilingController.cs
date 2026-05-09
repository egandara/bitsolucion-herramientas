using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NotebookValidator.Web.Services;
using NotebookValidator.Web.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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
        [RequestFormLimits(MultipartBodyLengthLimit = 2147483648)] // Permite hasta 2 GB
        [RequestSizeLimit(2147483648)] // Permite hasta 2 GB
        public async Task<IActionResult> Analyze(IFormFile archivo, bool tieneEncabezados = true)
        {
            if (archivo == null || archivo.Length == 0)
            {
                TempData["ToastMessage"] = "Por favor, selecciona un archivo válido.";
                TempData["ToastType"] = "error";
                return RedirectToAction("Index");
            }

            try
            {
                // Guardar temporalmente el archivo para permitir búsquedas posteriores de duplicados
                var tempFileName = $"{Guid.NewGuid()}{Path.GetExtension(archivo.FileName)}";
                var tempPath = Path.Combine(Path.GetTempPath(), tempFileName);

                using (var fs = System.IO.File.Create(tempPath))
                {
                    await archivo.CopyToAsync(fs);
                }

                // Analizar desde el fichero temporal
                using var readStream = System.IO.File.OpenRead(tempPath);
                var profileResult = _profilingService.AnalyzeFile(readStream, archivo.FileName, tieneEncabezados);

                // Guardar metadata para la vista (nombre temporal y si tenía encabezados)
                profileResult.TempFileName = tempFileName;
                profileResult.HasHeaders = tieneEncabezados;

                return View("Resultados", profileResult);
            }
            catch (Exception ex)
            {
                TempData["ToastMessage"] = $"Error al analizar el archivo: {ex.Message}";
                TempData["ToastType"] = "error";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public IActionResult FindDuplicates(string tempFileName, string[] keyColumns, bool fullRow = false, bool hasHeaders = true)
        {
            if (string.IsNullOrEmpty(tempFileName))
            {
                TempData["ToastMessage"] = "No se encontró el archivo temporal para realizar la búsqueda de duplicados.";
                TempData["ToastType"] = "error";
                return RedirectToAction("Index");
            }

            try
            {
                var tempPath = Path.Combine(Path.GetTempPath(), tempFileName);
                if (!System.IO.File.Exists(tempPath))
                {
                    TempData["ToastMessage"] = "El archivo temporal ha expirado o no existe.";
                    TempData["ToastType"] = "error";
                    return RedirectToAction("Index");
                }

                // Volver a generar el perfil (para mantener consistencia en la vista)
                using var readStream = System.IO.File.OpenRead(tempPath);
                var profileResult = _profilingService.AnalyzeFile(readStream, tempFileName, hasHeaders);

                // Ejecutar búsqueda de duplicados según la selección del usuario
                var duplicates = _profilingService.FindDuplicateGroups(tempPath, hasHeaders, keyColumns, fullRow);
                profileResult.DuplicateGroups = duplicates;

                // Mantener metadata para la vista
                profileResult.TempFileName = tempFileName;
                profileResult.HasHeaders = hasHeaders;

                return View("Resultados", profileResult);
            }
            catch (Exception ex)
            {
                TempData["ToastMessage"] = $"Error al buscar duplicados: {ex.Message}";
                TempData["ToastType"] = "error";
                return RedirectToAction("Index");
            }
        }

        // Nuevo endpoint AJAX para validar columnas Texto
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ValidateTextColumn(string tempFileName, string columnName, string targetType = null, bool hasHeaders = true)
        {
            if (string.IsNullOrEmpty(tempFileName) || string.IsNullOrEmpty(columnName))
            {
                return BadRequest("Parámetros incompletos.");
            }

            var tempPath = Path.Combine(Path.GetTempPath(), tempFileName);
            if (!System.IO.File.Exists(tempPath))
            {
                return NotFound("Archivo temporal no encontrado o expirado.");
            }

            try
            {
                // Llamada al servicio (sampling y mismatches limitados)
                var validation = _profilingService.ValidateTextColumn(tempPath, hasHeaders, columnName, sampleLimit: 5000, mismatchSampleLimit: 200, targetType: targetType);
                return Json(validation);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al validar la columna: {ex.Message}");
            }
        }
    }
}
