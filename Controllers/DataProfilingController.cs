using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using NotebookValidator.Web.Services;
using NotebookValidator.Web.Models;
using NotebookValidator.Web.Hubs;
using System;
using System.IO;
using System.Threading.Tasks;

namespace NotebookValidator.Web.Controllers
{
    [Authorize]
    public class DataProfilingController : Controller
    {
        private readonly DataProfilingService _profilingService;
        private readonly IMemoryCache _cache;
        private readonly IHubContext<ProfilingHub> _hubContext;

        public DataProfilingController(DataProfilingService profilingService, IMemoryCache cache, IHubContext<ProfilingHub> hubContext)
        {
            _profilingService = profilingService;
            _cache = cache;
            _hubContext = hubContext;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // NUEVO MÉTODO PARA VER LOS RESULTADOS DESDE LA CACHÉ
        [HttpGet]
        public IActionResult VerResultados(string jobId)
        {
            if (string.IsNullOrEmpty(jobId) || !_cache.TryGetValue(jobId, out DataProfileResult profileResult))
            {
                TempData["ToastrMessage"] = "El análisis ha expirado o el ID no es válido. Por favor, analiza el archivo nuevamente.";
                TempData["ToastrType"] = "error";
                return RedirectToAction("Index");
            }
            return View("Resultados", profileResult);
        }

        [HttpPost]
        [RequestFormLimits(MultipartBodyLengthLimit = 2147483648)]
        [RequestSizeLimit(2147483648)]
        public async Task<IActionResult> Analyze(IFormFile archivo, string connectionId, bool tieneEncabezados = true)
        {
            if (archivo == null || archivo.Length == 0) return BadRequest("Archivo inválido.");

            var jobId = Guid.NewGuid().ToString();
            var tempFileName = $"{Guid.NewGuid()}{Path.GetExtension(archivo.FileName)}";
            var tempPath = Path.Combine(Path.GetTempPath(), tempFileName);
            var originalFileName = archivo.FileName;
            var fileSize = archivo.Length;

            // 1. Guardamos el archivo rápidamente
            using (var fs = System.IO.File.Create(tempPath))
            {
                await archivo.CopyToAsync(fs);
            }

            // 2. Iniciamos el trabajo pesado en SEGUNDO PLANO (Fire and Forget)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveProgress", "Iniciando motor de procesamiento...");
                    var watch = System.Diagnostics.Stopwatch.StartNew();

                    using var readStream = System.IO.File.OpenRead(tempPath);
                    var profileResult = _profilingService.AnalyzeFile(readStream, originalFileName, tieneEncabezados);

                    profileResult.TempFileName = tempFileName;
                    profileResult.HasHeaders = tieneEncabezados;

                    double mb = fileSize / (1024.0 * 1024.0);
                    profileResult.FileSizeFormatted = mb >= 1024 ? $"{(mb / 1024.0):N2} GB" : $"{mb:N2} MB";

                    watch.Stop();
                    profileResult.ProcessingTimeFormatted = watch.Elapsed.TotalSeconds < 60
                        ? $"{watch.Elapsed.TotalSeconds:N1} seg"
                        : $"{(watch.Elapsed.TotalMinutes):N1} min";

                    // 3. Guardamos el resultado en memoria por 30 minutos
                    _cache.Set(jobId, profileResult, TimeSpan.FromMinutes(30));

                    // 4. Le avisamos al navegador que ya terminamos
                    await _hubContext.Clients.Client(connectionId).SendAsync("ProcessingComplete", jobId);
                }
                catch (Exception ex)
                {
                    await _hubContext.Clients.Client(connectionId).SendAsync("ProcessingError", ex.Message);
                }
            });

            // Soltamos la conexión HTTP de inmediato para evitar Timeouts
            return Json(new { success = true, jobId = jobId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult FindDuplicates(string tempFileName, string[] keyColumns, bool fullRow = false, bool hasHeaders = true, string fileSize = "", string processingTime = "")
        {
            if (string.IsNullOrEmpty(tempFileName)) return RedirectToAction("Index");

            try
            {
                var tempPath = Path.Combine(Path.GetTempPath(), tempFileName);
                if (!System.IO.File.Exists(tempPath)) return RedirectToAction("Index");

                using var readStream = System.IO.File.OpenRead(tempPath);
                var profileResult = _profilingService.AnalyzeFile(readStream, tempFileName, hasHeaders);

                var duplicates = _profilingService.FindDuplicateGroups(tempPath, hasHeaders, keyColumns, fullRow);
                profileResult.DuplicateGroups = duplicates;

                profileResult.TempFileName = tempFileName;
                profileResult.HasHeaders = hasHeaders;
                profileResult.FileSizeFormatted = fileSize;
                profileResult.ProcessingTimeFormatted = processingTime;
                profileResult.FileExtension = Path.GetExtension(tempFileName).ToUpper().Replace(".", "");

                return Json(new { success = true, duplicateGroups = profileResult.DuplicateGroups });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ValidateTextColumn(string tempFileName, string columnName, string targetType = null, bool hasHeaders = true)
        {
            if (string.IsNullOrEmpty(tempFileName) || string.IsNullOrEmpty(columnName)) return BadRequest();
            var tempPath = Path.Combine(Path.GetTempPath(), tempFileName);
            if (!System.IO.File.Exists(tempPath)) return NotFound();

            var validation = _profilingService.ValidateTextColumn(tempPath, hasHeaders, columnName, sampleLimit: 5000, mismatchSampleLimit: 200, targetType: targetType);
            return Json(validation);
        }
    }
}
