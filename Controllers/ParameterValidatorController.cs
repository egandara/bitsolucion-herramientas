using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotebookValidator.Web.Data;
using NotebookValidator.Web.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace NotebookValidator.Web.Controllers
{
    [Authorize(Roles = "Admin, ParameterValidatorUser")]
    public class ParameterValidatorController : Controller
    {
        private readonly ParameterValidationService _service;
        private readonly ApplicationDbContext _context;

        public ParameterValidatorController(ParameterValidationService service, ApplicationDbContext context)
        {
            _service = service;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            TempData.Remove("AnalysisToken"); // Limpiar análisis anteriores
            await LoadMasterParametersAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Validate(IFormFileCollection files)
        {
            if (files == null || files.Count == 0)
            {
                ViewBag.Error = "Por favor, selecciona al menos un archivo.";
                await LoadMasterParametersAsync();
                return View("Index");
            }

            string analysisToken = Guid.NewGuid().ToString("N");
            string cachePath = Path.Combine(Path.GetTempPath(), "BitSolucion_AnalysisCache", analysisToken);

            var (findings, summaries) = await _service.ValidateAndCacheParametersAsync(files, cachePath);

            TempData["AnalysisToken"] = analysisToken;
            ViewBag.FileSummaries = summaries;

            await LoadMasterParametersAsync();
            return View("Index", findings);
        }

        // ENDPOINT ACTUALIZADO: Soporta corrección individual o masiva asíncrona (Bulk Fix)
        [HttpPost]
        public async Task<IActionResult> ApplyCorrection(string oldParam, string newParam, string notebookName)
        {
            string token = TempData.Peek("AnalysisToken")?.ToString();
            if (string.IsNullOrEmpty(token)) return Json(new { success = false, message = "Sesión de análisis expirada." });

            string cacheFolder = Path.Combine(Path.GetTempPath(), "BitSolucion_AnalysisCache", token);
            if (!Directory.Exists(cacheFolder)) return Json(new { success = false, message = "Caché de análisis no encontrada." });

            try
            {
                List<string> modifiedFiles = new List<string>();

                // Escenario A: Se especificó un notebook (Corrección dirigida tradicional)
                if (!string.IsNullOrEmpty(notebookName))
                {
                    string filePath = Path.Combine(cacheFolder, notebookName);
                    if (System.IO.File.Exists(filePath))
                    {
                        string content = await System.IO.File.ReadAllTextAsync(filePath);
                        string updatedContent = content.Replace(oldParam, newParam);
                        await System.IO.File.WriteAllTextAsync(filePath, updatedContent);
                        modifiedFiles.Add(notebookName);
                    }
                }
                // Escenario B: Nombre de notebook vacío (OPERACIÓN MASIVA BULK EN CALIENTE)
                else
                {
                    var cachedFiles = Directory.GetFiles(cacheFolder, "*.*", SearchOption.AllDirectories)
                                               .Where(f => f.EndsWith(".py") || f.EndsWith(".ipynb"));

                    foreach (var file in cachedFiles)
                    {
                        string content = await System.IO.File.ReadAllTextAsync(file);
                        if (content.Contains(oldParam))
                        {
                            string updatedContent = content.Replace(oldParam, newParam);
                            await System.IO.File.WriteAllTextAsync(file, updatedContent);
                            modifiedFiles.Add(Path.GetFileName(file));
                        }
                    }
                }

                return Json(new
                {
                    success = true,
                    paramName = oldParam,
                    newParamName = newParam,
                    isBulk = string.IsNullOrEmpty(notebookName),
                    files = modifiedFiles
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al editar los archivos físicos: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult DownloadCorrectedFiles()
        {
            string token = TempData.Peek("AnalysisToken")?.ToString();
            if (string.IsNullOrEmpty(token)) return BadRequest("No existe un análisis activo para descargar.");

            string cacheFolder = Path.Combine(Path.GetTempPath(), "BitSolucion_AnalysisCache", token);
            if (!Directory.Exists(cacheFolder) || !Directory.GetFiles(cacheFolder).Any())
                return NotFound("Los archivos del análisis ya no se encuentran disponibles.");

            string zipPath = Path.Combine(Path.GetTempPath(), $"Solucion_Corregida_{DateTime.Now:yyyyMMddHHmmss}.zip");

            if (System.IO.File.Exists(zipPath)) System.IO.File.Delete(zipPath);
            ZipFile.CreateFromDirectory(cacheFolder, zipPath);

            var fileBytes = System.IO.File.ReadAllBytes(zipPath);
            System.IO.File.Delete(zipPath);

            return File(fileBytes, "application/zip", Path.GetFileName(zipPath));
        }

        private async Task LoadMasterParametersAsync()
        {
            var allowedParams = await _context.AllowedParameters.OrderBy(p => p.Name.ToLower()).ToListAsync();
            ViewBag.AllowedParameters = allowedParams;
            ViewBag.TotalParameters = allowedParams.Count;
        }
    }
}
