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
        public async Task<IActionResult> PrepareStaging(List<IFormFile> files, string certSparkConf, string prodSparkConf, bool masterAutocert = false)
        {
            if (files == null || !files.Any())
            {
                return Json(new { success = false, message = "Por favor, selecciona al menos un archivo de configuración o ZIP." });
            }

            string token = Guid.NewGuid().ToString("N");
            string cachePath = Path.Combine(Path.GetTempPath(), "BitSolucion_JobCache", token);
            Directory.CreateDirectory(cachePath);

            var stagedJobs = new List<object>();
            var regexJsonName = new Regex(@"""name""\s*:\s*""([^""]+)""");
            var regexYamlName = new Regex(@"name:\s*[""']?([^""'\n]+)[""']?");

            var zipFiles = files.Where(f => f.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)).ToList();
            var configFiles = files.Where(f => f.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                                               f.FileName.EndsWith(".yml", StringComparison.OrdinalIgnoreCase) ||
                                               f.FileName.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase)).ToList();

            var rawSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var rawTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (masterAutocert && zipFiles.Any())
            {
                foreach (var zip in zipFiles)
                {
                    using var archive = new ZipArchive(zip.OpenReadStream(), ZipArchiveMode.Read);
                    foreach (var entry in archive.Entries)
                    {
                        if (entry.FullName.EndsWith(".py", StringComparison.OrdinalIgnoreCase) ||
                            entry.FullName.EndsWith(".sql", StringComparison.OrdinalIgnoreCase) ||
                            entry.FullName.EndsWith(".scala", StringComparison.OrdinalIgnoreCase))
                        {
                            using var stream = entry.Open();
                            using var reader = new StreamReader(stream);
                            var content = await reader.ReadToEndAsync();

                            content = Regex.Replace(content, @"[""']{1,3}\s*\+\s*([a-zA-Z0-9_]+)\s*\+\s*[""']{1,3}", "${$1}");

                            foreach (Match m in Regex.Matches(content, @"(?i)(?:FROM|JOIN)\s+([`a-zA-Z0-9_\.\{\}\$]+)"))
                            {
                                string match = m.Groups[1].Value.Replace("`", "").Trim();
                                if (!match.Equals("SELECT", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(match))
                                    rawSources.Add(match);
                            }
                            foreach (Match m in Regex.Matches(content, @"(?i)spark\.(?:read\.)?table\s*\(\s*f?[""']([^""']+)[""']\s*\)"))
                                rawSources.Add(m.Groups[1].Value);

                            foreach (Match m in Regex.Matches(content, @"(?i)(?:saveAsTable|insertInto)\s*\(\s*f?[""']([^""']+)[""']\s*\)"))
                                rawTargets.Add(m.Groups[1].Value);

                            foreach (Match m in Regex.Matches(content, @"(?i)(?:INSERT\s+(?:INTO|OVERWRITE)|MERGE\s+INTO|CREATE\s+(?:OR\s+REPLACE\s+)?TABLE\s+(?:IF\s+NOT\s+EXISTS)?)\s+([`a-zA-Z0-9_\.\{\}\$]+)"))
                            {
                                string match = m.Groups[1].Value.Replace("`", "").Trim();
                                if (!string.IsNullOrWhiteSpace(match)) rawTargets.Add(match);
                            }
                        }
                    }
                }
            }

            Func<string, bool> isTempOrJunk = s =>
            {
                string low = s.ToLower();
                return low.Contains("tmp") ||
                       low.Contains("temp") ||
                       low.Contains("vista") ||
                       low.StartsWith("v_") ||
                       low.StartsWith("vw_") ||
                       low.StartsWith("vt_") ||
                       low.Equals("dual") ||
                       low.Length <= 3 ||
                       !Regex.IsMatch(low, "[a-z]") ||
                       !(s.Contains(".") || s.Contains("{") || s.Contains("$"));
            };

            var cleanSources = rawSources.Where(x => !isTempOrJunk(x)).ToList();
            var cleanTargets = rawTargets.Where(x => !isTempOrJunk(x)).ToList();

            foreach (var file in configFiles)
            {
                string ext = Path.GetExtension(file.FileName).ToLower();
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

                var parametersList = new List<string>();
                if (ext == ".yml" || ext == ".yaml")
                {
                    var paramMatches = Regex.Matches(content, @"-\s*name:\s*([A-Za-z0-9_]+)[\s\r\n]*default:");
                    foreach (Match m in paramMatches)
                    {
                        if (!parametersList.Contains(m.Groups[1].Value))
                        {
                            parametersList.Add(m.Groups[1].Value);
                        }
                    }
                }

                var autoSources = new List<object>();
                var autoTargets = new List<object>();

                if (masterAutocert)
                {
                    foreach (var s in cleanSources)
                    {
                        var parsed = ParseTableString(s, parametersList);
                        autoSources.Add(new { param = parsed.param, table = parsed.table });
                    }
                    foreach (var t in cleanTargets)
                    {
                        var parsed = ParseTableString(t, parametersList);
                        autoTargets.Add(new { param = parsed.param, table = parsed.table });
                    }
                }

                stagedJobs.Add(new
                {
                    fileKey = fileKey,
                    originalFileName = file.FileName,
                    originalJobName = originalJobName,
                    suggestedDevName = cleanedName,
                    suggestedCertName = cleanedName,
                    suggestedProdName = cleanedName,
                    isYaml = (ext == ".yml" || ext == ".yaml"),
                    parameters = parametersList,
                    autoSources = autoSources,
                    autoTargets = autoTargets
                });
            }

            TempData["JobAnalysisToken"] = token;
            TempData["CertSparkConfGlobal"] = certSparkConf ?? "";
            TempData["ProdSparkConfGlobal"] = prodSparkConf ?? "";

            return Json(new { success = true, token = token, jobs = stagedJobs });
        }

        private (string param, string table) ParseTableString(string raw, List<string> knownParams)
        {
            string cleaned = raw.Replace("{", "").Replace("}", "").Replace("$", "").Replace("`", "");

            if (cleaned.Contains("."))
            {
                var parts = cleaned.Split('.');
                string possibleParam = parts[0];
                string tablePart = string.Join(".", parts.Skip(1));

                var exactParam = knownParams.FirstOrDefault(p => p.Equals(possibleParam, StringComparison.OrdinalIgnoreCase));

                if (exactParam == null)
                {
                    string cleanPossible = Regex.Replace(possibleParam, @"(?i)_?[xw]$", "");
                    exactParam = knownParams.FirstOrDefault(p =>
                        Regex.Replace(p, @"(?i)_?[xw]$", "").Equals(cleanPossible, StringComparison.OrdinalIgnoreCase));
                }

                if (exactParam != null)
                {
                    return (exactParam, tablePart);
                }

                return ("", cleaned);
            }

            return ("", cleaned);
        }

        // ===================================================================================================
        // CORREGIDO: Inyección de BundleName y separación de nombres por entorno (Dev, Cert, Prod)
        // ===================================================================================================
        [HttpPost]
        public async Task<IActionResult> DownloadPackage(
            List<string> fileKeys,
            List<string> devNames,
            List<string> certNames,
            List<string> prodNames,
            List<string> permLevels,
            List<string> permUsers,
            List<string> devAutocert,
            List<string> certAutocert,
            List<string> prodAutocert,
            List<string> sourceTables,
            List<string> targetTables,
            string bundleName) // <--- Nuevo parámetro enviado desde la vista Fase 1
        {
            string token = TempData.Peek("JobAnalysisToken")?.ToString();
            if (string.IsNullOrEmpty(token)) return BadRequest("La sesión de configuración de Jobs ha expirado.");

            string cacheFolder = Path.Combine(Path.GetTempPath(), "BitSolucion_JobCache", token);
            if (!Directory.Exists(cacheFolder)) return NotFound("Los archivos del pipeline de configuración ya no están disponibles.");

            string certSparkConf = TempData["CertSparkConfGlobal"]?.ToString() ?? "";
            string prodSparkConf = TempData["ProdSparkConfGlobal"]?.ToString() ?? "";

            var regexNameReplace = new Regex(@"""name""\s*:\s*""[^""]*""");

            var yamlContents = new List<string>();
            var yamlDevNames = new List<string>();
            var yamlCertNames = new List<string>();
            var yamlProdNames = new List<string>();
            var yamlDevAutocerts = new List<bool>();
            var yamlCertAutocerts = new List<bool>();
            var yamlProdAutocerts = new List<bool>();
            var yamlSourceTables = new List<string>();
            var yamlTargetTables = new List<string>();

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
                                bool dAuto = (devAutocert != null && devAutocert.Count > i) && devAutocert[i] == "true";
                                bool cAuto = (certAutocert != null && certAutocert.Count > i) && certAutocert[i] == "true";
                                bool pAuto = (prodAutocert != null && prodAutocert.Count > i) && prodAutocert[i] == "true";

                                yamlContents.Add(rawContent);
                                yamlDevNames.Add(devNames[i]);
                                yamlCertNames.Add(certNames[i]);
                                yamlProdNames.Add(prodNames[i]);
                                yamlDevAutocerts.Add(dAuto);
                                yamlCertAutocerts.Add(cAuto);
                                yamlProdAutocerts.Add(pAuto);

                                yamlSourceTables.Add(sourceTables != null && sourceTables.Count > i ? sourceTables[i] : "");
                                yamlTargetTables.Add(targetTables != null && targetTables.Count > i ? targetTables[i] : "");
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

                        if (yamlContents.Count > 0)
                        {
                            // Inyección dinámica de todos los arreglos separados por entorno y el nombre del bundle
                            var bundleConfigs = _transformationService.GenerateBundleConfigs(
                                yamlContents, yamlDevNames, yamlCertNames, yamlProdNames,
                                permLevels, permUsers,
                                yamlDevAutocerts, yamlCertAutocerts, yamlProdAutocerts,
                                yamlSourceTables, yamlTargetTables, bundleName);

                            foreach (var config in bundleConfigs)
                            {
                                WriteZipEntry(archive, config.Key, config.Value);
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
