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
            TempData.Remove("JobAnalysisToken");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> PrepareStaging(List<IFormFile> files, string certSparkConf, string prodSparkConf)
        {
            if (files == null || !files.Any())
            {
                return Json(new { success = false, message = "Por favor, selecciona al menos un archivo JSON o YAML de Job." });
            }

            string token = Guid.NewGuid().ToString("N");
            string cachePath = Path.Combine(Path.GetTempPath(), "BitSolucion_JobCache", token);
            Directory.CreateDirectory(cachePath);

            var stagedJobs = new List<object>();
            var regexJsonName = new Regex(@"""name""\s*:\s*""([^""]+)""");
            var regexYamlName = new Regex(@"name:\s*[""']?([^""'\n]+)[""']?");

            foreach (var file in files)
            {
                string ext = Path.GetExtension(file.FileName).ToLower();
                if (ext != ".json" && ext != ".yml" && ext != ".yaml") continue;

                string fileKey = Guid.NewGuid().ToString("N") + ext;
                string fullPath = Path.Combine(cachePath, fileKey);

                string content;
                using (var reader = new StreamReader(file.OpenReadStream()))
                {
                    content = await reader.ReadToEndAsync();
                }

                await System.IO.File.WriteAllTextAsync(fullPath, content);

                string originalJobName = "";
                if (ext == ".json")
                {
                    var match = regexJsonName.Match(content);
                    originalJobName = match.Success ? match.Groups[1].Value : Path.GetFileNameWithoutExtension(file.FileName);
                }
                else
                {
                    var match = regexYamlName.Match(content);
                    originalJobName = match.Success ? match.Groups[1].Value : Path.GetFileNameWithoutExtension(file.FileName);
                }

                string cleanedName = Regex.Replace(originalJobName, @"^\[dev\]|^\[DEV\]|^dev_|^dev-", "", RegexOptions.IgnoreCase).Trim();

                stagedJobs.Add(new
                {
                    fileKey = fileKey,
                    originalFileName = file.FileName,
                    originalJobName = originalJobName,
                    suggestedDevName = cleanedName,
                    suggestedCertName = cleanedName,
                    suggestedProdName = cleanedName,
                    isYaml = (ext == ".yml" || ext == ".yaml")
                });
            }

            TempData["JobAnalysisToken"] = token;
            TempData["CertSparkConfGlobal"] = certSparkConf ?? "";
            TempData["ProdSparkConfGlobal"] = prodSparkConf ?? "";

            return Json(new { success = true, token = token, jobs = stagedJobs });
        }

        [HttpPost]
        public async Task<IActionResult> DownloadPackage(
            List<string> fileKeys,
            List<string> devNames,
            List<string> certNames,
            List<string> prodNames,
            List<string> permLevels,
            List<string> permUsers,
            List<string> devAutocert,    // NUEVO: Recepción de flags booleanos mapeados desde la vista
            List<string> certAutocert,   // NUEVO: Recepción de flags booleanos mapeados desde la vista
            List<string> prodAutocert)   // NUEVO: Recepción de flags booleanos mapeados desde la vista
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

                            string rawContent = await System.IO.File.ReadAllTextAsync(filePath);
                            bool isYaml = fileKey.EndsWith(".yml") || fileKey.EndsWith(".yaml");

                            if (isYaml)
                            {
                                // Inyección de los valores individuales de Autocert seleccionados por el usuario
                                bool dAuto = (devAutocert != null && devAutocert.Count > i) && devAutocert[i] == "true";
                                bool cAuto = (certAutocert != null && certAutocert.Count > i) && certAutocert[i] == "true";
                                bool pAuto = (prodAutocert != null && prodAutocert.Count > i) && prodAutocert[i] == "true";

                                var bundleConfigs = _transformationService.GenerateBundleConfigs(rawContent, devNames[i], permLevels, permUsers, dAuto, cAuto, pAuto);

                                foreach (var config in bundleConfigs)
                                {
                                    WriteZipEntry(archive, config.Key, config.Value);
                                }
                            }
                            else
                            {
                                var envConfigs = _transformationService.GenerateEnvironmentConfigs(rawContent, certSparkConf, prodSparkConf);

                                string devJsonFinal = regexNameReplace.Replace(envConfigs["Desarrollo.json"], $"\"name\": \"{devNames[i]}\"", 1);
                                string certJsonFinal = regexNameReplace.Replace(envConfigs["Certificacion.json"], $"\"name\": \"{certNames[i]}\"", 1);
                                string prodJsonFinal = regexNameReplace.Replace(envConfigs["Produccion.json"], $"\"name\": \"{prodNames[i]}\"", 1);

                                WriteZipEntry(archive, $"Desarrollo/{devNames[i].Replace(" ", "_")}.json", devJsonFinal);
                                WriteZipEntry(archive, $"Certificacion/{certNames[i].Replace(" ", "_")}.json", certJsonFinal);
                                WriteZipEntry(archive, $"Produccion/{prodNames[i].Replace(" ", "_")}.json", prodJsonFinal);
                            }
                        }
                    }

                    if (Directory.Exists(cacheFolder)) Directory.Delete(cacheFolder, true);
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
