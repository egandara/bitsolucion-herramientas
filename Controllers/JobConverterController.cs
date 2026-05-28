using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NotebookValidator.Web.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
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

        // Clase auxiliar para el ordenamiento inteligente
        private class StagedJobInfo
        {
            public string FileKey { get; set; }
            public string OriginalFileName { get; set; }
            public string OriginalJobName { get; set; }
            public string CleanedName { get; set; }
            public bool IsYaml { get; set; }
            public List<string> ParametersList { get; set; }
            public List<object> AutoSources { get; set; }
            public List<object> AutoTargets { get; set; }
            public string InferredBundleName { get; set; }
            public bool IsProvisioning { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> PrepareStaging(List<IFormFile> files, string certSparkConf, string prodSparkConf, bool masterAutocert = false)
        {
            if (files == null || !files.Any())
                return Json(new { success = false, message = "Por favor, selecciona al menos un archivo de configuración o ZIP." });

            string token = Guid.NewGuid().ToString("N");
            string cachePath = Path.Combine(Path.GetTempPath(), "BitSolucion_JobCache", token);
            Directory.CreateDirectory(cachePath);

            var stagedJobs = new List<object>();
            var tempStagedJobs = new List<StagedJobInfo>();

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
                return low.Contains("tmp") || low.Contains("temp") || low.Contains("vista") ||
                       low.StartsWith("v_") || low.StartsWith("vw_") || low.StartsWith("vt_") ||
                       low.Equals("dual") || low.Length <= 3 || !Regex.IsMatch(low, "[a-z]") ||
                       !(s.Contains(".") || s.Contains("{") || s.Contains("$"));
            };

            var cleanSources = rawSources.Where(x => !isTempOrJunk(x)).ToList();
            var cleanTargets = rawTargets.Where(x => !isTempOrJunk(x)).ToList();

            var provisioningKeywords = new[] { "creacion", "create", "tablas", "aprovisionamiento", "despliegue", "tables" };

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

                // Detección de prioridades y etiquetado normativo de aprovisionamiento
                bool isProv = provisioningKeywords.Any(k => originalJobName.Contains(k, StringComparison.OrdinalIgnoreCase) || file.FileName.Contains(k, StringComparison.OrdinalIgnoreCase));

                if (isProv && !cleanedName.EndsWith("_aprovisionamiento", StringComparison.OrdinalIgnoreCase))
                {
                    cleanedName += "_aprovisionamiento";
                }

                string inferredBundleName = "";
                if (ext == ".yml" || ext == ".yaml")
                {
                    var repoMatch = Regex.Match(content, @"(?i)(?:\.bundles/|/Repos/[^/]+/|/Shared/)([^/]+)/");
                    if (repoMatch.Success)
                    {
                        inferredBundleName = repoMatch.Groups[1].Value;
                    }
                    else
                    {
                        var bundleMatch = Regex.Match(content, @"(?i)bundle:\s*\n\s*name:\s*([^\s]+)");
                        if (bundleMatch.Success) inferredBundleName = bundleMatch.Groups[1].Value;
                    }
                }

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

                tempStagedJobs.Add(new StagedJobInfo
                {
                    FileKey = fileKey,
                    OriginalFileName = file.FileName,
                    OriginalJobName = originalJobName,
                    CleanedName = cleanedName,
                    IsYaml = (ext == ".yml" || ext == ".yaml"),
                    ParametersList = parametersList,
                    AutoSources = autoSources,
                    AutoTargets = autoTargets,
                    InferredBundleName = inferredBundleName,
                    IsProvisioning = isProv
                });
            }

            // Ordenamiento Mágico: Aprovisionamiento de primero.
            var sortedJobs = tempStagedJobs.OrderByDescending(j => j.IsProvisioning).ThenBy(j => j.OriginalFileName).ToList();

            foreach (var j in sortedJobs)
            {
                stagedJobs.Add(new
                {
                    fileKey = j.FileKey,
                    originalFileName = j.OriginalFileName,
                    originalJobName = j.OriginalJobName,
                    suggestedDevName = j.CleanedName,
                    suggestedCertName = j.CleanedName,
                    suggestedProdName = j.CleanedName,
                    isYaml = j.IsYaml,
                    parameters = j.ParametersList,
                    autoSources = j.AutoSources,
                    autoTargets = j.AutoTargets,
                    inferredBundleName = j.InferredBundleName,
                    isProvisioning = j.IsProvisioning
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

                if (exactParam != null) return (exactParam, tablePart);
                return ("", cleaned);
            }
            return ("", cleaned);
        }

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
            string bundleName)
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
                                yamlDevNames.Add(devNames[i].Replace(".yml", "").Replace(".yaml", ""));
                                yamlCertNames.Add(certNames[i].Replace(".yml", "").Replace(".yaml", ""));
                                yamlProdNames.Add(prodNames[i].Replace(".yml", "").Replace(".yaml", ""));
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
                            var bundleConfigs = _transformationService.GenerateBundleConfigs(
                                yamlContents, yamlDevNames, yamlCertNames, yamlProdNames,
                                permLevels, permUsers,
                                yamlDevAutocerts, yamlCertAutocerts, yamlProdAutocerts,
                                yamlSourceTables, yamlTargetTables, bundleName);

                            foreach (var config in bundleConfigs)
                            {
                                string path = config.Key;
                                string content = config.Value;

                                path = path.Replace(".yml.yml", ".yml").Replace(".yaml.yml", ".yml").Replace(".yml.scala", ".scala");

                                if (path.EndsWith(".scala", StringComparison.OrdinalIgnoreCase))
                                {
                                    string directory = Path.GetDirectoryName(path);
                                    string fileName = Path.GetFileName(path);
                                    string newFileName = Regex.Replace(fileName, @"(?i)\d{3}-(?:qa|noqa)-(?:run|norun)-", "");
                                    path = string.IsNullOrEmpty(directory) ? newFileName : Path.Combine(directory, newFileName).Replace("\\", "/");
                                }
                                else if (path.EndsWith("databricks.yml"))
                                {
                                    string devPermsStr = "jobPermissions:\n";
                                    if (permLevels != null && permUsers != null)
                                    {
                                        for (int p = 0; p < permLevels.Count; p++)
                                        {
                                            string key = permUsers[p].Contains("GRP") ? "group_name" : "user_name";
                                            devPermsStr += $"        - level: {permLevels[p]}\n          {key}: {permUsers[p]}\n";
                                        }
                                    }

                                    string certPermsStr = "jobPermissions:\n        - level: CAN_MANAGE\n          user_name: cmoreab@bci.cl\n        - level: CAN_MANAGE\n          user_name: mcordof@bci.cl\n";
                                    string prodPermsStr = "jobPermissions:\n        - level: CAN_MANAGE\n          group_name: GRP_AZURE_DATABRICKS_PRODUCCION_BIGDATA_PRD\n";

                                    content = UpdatePermissions(content, "develop", devPermsStr);
                                    content = UpdatePermissions(content, "certification", certPermsStr);
                                    content = UpdatePermissions(content, "production", prodPermsStr);

                                    foreach (var fullJobName in yamlDevNames)
                                    {
                                        string cleanJobName = Regex.Replace(fullJobName, @"^\d{3}-(?:qa|noqa)-(?:run|norun)-", "");

                                        // La llave de la variable limpia de _aprovisionamiento
                                        string variableKeyName = Regex.Replace(cleanJobName, @"(?i)_aprovisionamiento$", "");

                                        content = content.Replace($"jobName_{fullJobName}", $"jobName_{variableKeyName}");

                                        string varKey = $"jobName_{variableKeyName}";
                                        content = Regex.Replace(content, $@"{varKey}:\s*\n\s+description:.*?\n\s+default:\s*(?:""{fullJobName}""|{fullJobName})",
                                            $"{varKey}:\n    description: {variableKeyName}\n    default: ${{bundle.target}}-{cleanJobName}");
                                        content = Regex.Replace(content, $@"{varKey}:\s*""{fullJobName}""", $"{varKey}: {cleanJobName}");
                                    }
                                }
                                else if (path.Contains("resources/") && path.EndsWith(".yml"))
                                {
                                    string fileNameWithoutExt = Path.GetFileNameWithoutExtension(path);
                                    string cleanJobName = Regex.Replace(fileNameWithoutExt, @"^\d{3}-(?:qa|noqa)-(?:run|norun)-", "");
                                    string variableKeyName = Regex.Replace(cleanJobName, @"(?i)_aprovisionamiento$", "");

                                    content = Regex.Replace(content, $@"([ \t]+){Regex.Escape(fileNameWithoutExt)}:\s*\n", $"$1{cleanJobName}:\n");

                                    var nameRegex = new Regex(@"name:\s*""?[^""\n]+""?");
                                    content = nameRegex.Replace(content, $"name: ${{var.jobName_{variableKeyName}}}", 1);

                                    string badTaskRegex = @"[ \t]+-[ \t]+task_key:\s*Auto_Certificacion\s*\r?\n(?:[ \t]+.*?\r?\n)*?(?=[ \t]+-[ \t]+task_key:|$)";
                                    content = Regex.Replace(content, badTaskRegex, "");

                                    content = Regex.Replace(content, @"(notebook_path:.*?)\d{3}-(?:qa|noqa)-(?:run|norun)-", "$1");

                                    // Corrección del parámetro 'nombreJob' para usar siempre la variable limpia
                                    content = Regex.Replace(content, @"(nombreJob:\s*""\$\{?var\.jobName_)[^""\}]+(\}?"")", $"$1{variableKeyName}$2");
                                }

                                WriteZipEntry(archive, path, content);
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

        private string UpdatePermissions(string content, string env, string newPerms)
        {
            string envPattern = $@"(?s)({env}:.*?)(?=^\s{{2}}[a-zA-Z0-9_-]+:|\z)";
            return Regex.Replace(content, envPattern, m =>
            {
                string block = m.Groups[1].Value;
                string permPattern = @"jobPermissions:\s*\n(?:[ \t]+-[^\n]+\n(?:[ \t]+(?:user_name|group_name):[^\n]+\n)*)*";

                if (Regex.IsMatch(block, permPattern))
                {
                    return Regex.Replace(block, permPattern, newPerms);
                }
                return block;
            }, RegexOptions.Multiline);
        }
    }
}
