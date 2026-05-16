using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NotebookValidator.Web.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NotebookValidator.Web.Controllers
{
    public class JobConverterController : Controller
    {
        private readonly JobTransformationService _transformationService;

        public JobConverterController(JobTransformationService transformationService)
        {
            _transformationService = transformationService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            TempData.Remove("JobAnalysisToken"); // Limpiar sesiones de jobs previas
            return View();
        }

        // FASE 1: Recibe los archivos múltiples y los almacena temporalmente en caché de sesión
        [HttpPost]
        public async Task<IActionResult> PrepareStaging(List<IFormFile> files, string certSparkConf, string prodSparkConf)
        {
            if (files == null || !files.Any())
            {
                return Json(new { success = false, message = "Por favor, selecciona al menos un archivo JSON de Job." });
            }

            string token = Guid.NewGuid().ToString("N");
            string cachePath = Path.Combine(Path.GetTempPath(), "BitSolucion_JobCache", token);
            Directory.CreateDirectory(cachePath);

            var stagedJobs = new List<object>();
            var regexName = new Regex(@"""name""\s*:\s*""([^""]+)""");

            foreach (var file in files)
            {
                if (!Path.GetExtension(file.FileName).Equals(".json", StringComparison.OrdinalIgnoreCase)) continue;

                string fileKey = Guid.NewGuid().ToString("N") + ".json";
                string fullPath = Path.Combine(cachePath, fileKey);

                string jsonContent;
                using (var reader = new StreamReader(file.OpenReadStream()))
                {
                    jsonContent = await reader.ReadToEndAsync();
                }

                // Guardar el original en la caché de trabajo
                await System.IO.File.WriteAllTextAsync(fullPath, jsonContent);

                // Intentar extraer el nombre interno del Job Databricks vía Regex
                var match = regexName.Match(jsonContent);
                string originalJobName = match.Success ? match.Groups[1].Value : Path.GetFileNameWithoutExtension(file.FileName);

                // Algoritmo de limpieza inteligente: remueve [dev], [DEV], dev_, dev- al inicio
                string cleanedName = Regex.Replace(originalJobName, @"^\[dev\]|^\[DEV\]|^dev_|^dev-", "", RegexOptions.IgnoreCase).Trim();

                // CORRECCIÓN: Los tres ambientes sugieren exactamente el mismo nombre limpio por defecto
                stagedJobs.Add(new
                {
                    fileKey = fileKey,
                    originalFileName = file.FileName,
                    originalJobName = originalJobName,
                    suggestedDevName = cleanedName,
                    suggestedCertName = cleanedName,
                    suggestedProdName = cleanedName
                });
            }

            // Persistir los configs globales en TempData para la fase de descarga final
            TempData["JobAnalysisToken"] = token;
            TempData["CertSparkConfGlobal"] = certSparkConf ?? "";
            TempData["ProdSparkConfGlobal"] = prodSparkConf ?? "";

            return Json(new { success = true, token = token, jobs = stagedJobs });
        }

        // FASE 3: Aplica los nombres confirmados por el usuario, corre el servicio y descarga el ZIP
        [HttpPost]
        public async Task<IActionResult> DownloadPackage(List<string> fileKeys, List<string> devNames, List<string> certNames, List<string> prodNames)
        {
            string token = TempData.Peek("JobAnalysisToken")?.ToString();
            if (string.IsNullOrEmpty(token)) return BadRequest("La sesión de configuración de Jobs ha expirado.");

            string cacheFolder = Path.Combine(Path.GetTempPath(), "BitSolucion_JobCache", token);
            if (!Directory.Exists(cacheFolder)) return NotFound("Los archivos del pipeline de configuración ya no están disponibles.");

            string certSparkConf = TempData["CertSparkConfGlobal"]?.ToString() ?? "";
            string prodSparkConf = TempData["ProdSparkConfGlobal"]?.ToString() ?? "";

            var regexNameReplace = new Regex(@"""name""\s*:\s*""[^""]*""");

            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                    {
                        for (int i = 0; i < fileKeys.Count; i++)
                        {
                            string fileKey = fileKeys[i];
                            string filePath = Path.Combine(cacheFolder, fileKey);

                            if (!System.IO.File.Exists(filePath)) continue;

                            string rawJson = await System.IO.File.ReadAllTextAsync(filePath);

                            // 1. Invocar tu servicio original para obtener las transformaciones base de rutas y storage
                            var envConfigs = _transformationService.GenerateEnvironmentConfigs(rawJson, certSparkConf, prodSparkConf);

                            // 2. Modificar dinámicamente la propiedad raíz "name" del JSON con las confirmaciones del formulario
                            string devJsonFinal = regexNameReplace.Replace(envConfigs["Desarrollo.json"], $"\"name\": \"{devNames[i]}\"", 1);
                            string certJsonFinal = regexNameReplace.Replace(envConfigs["Certificacion.json"], $"\"name\": \"{certNames[i]}\"", 1);
                            string prodJsonFinal = regexNameReplace.Replace(envConfigs["Produccion.json"], $"\"name\": \"{prodNames[i]}\"", 1);

                            // 3. Crear estructura de carpetas organizadas dentro del ZIP usando el nombre asignado a cada ambiente
                            WriteZipEntry(archive, $"Desarrollo/{devNames[i].Replace(" ", "_")}.json", devJsonFinal);
                            WriteZipEntry(archive, $"Certificacion/{certNames[i].Replace(" ", "_")}.json", certJsonFinal);
                            WriteZipEntry(archive, $"Produccion/{prodNames[i].Replace(" ", "_")}.json", prodJsonFinal);
                        }
                    }

                    // Limpieza de caché
                    Directory.Delete(cacheFolder, true);

                    return File(memoryStream.ToArray(), "application/zip", $"Pipeline_Jobs_BCI_{DateTime.Now:yyyyMMdd}.zip");
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error crítico en el empaquetador del pipeline: {ex.Message}");
                return RedirectToAction(nameof(Index));
            }
        }

        private void WriteZipEntry(ZipArchive archive, string path, string content)
        {
            var entry = archive.CreateEntry(path);
            using (var stream = entry.Open())
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(content);
            }
        }
    }
}
