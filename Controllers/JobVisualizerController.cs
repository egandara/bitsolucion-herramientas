using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace NotebookValidator.Web.Controllers
{
    [Authorize]
    public class JobVisualizerController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EscanearNotebookDesdeZip(IFormFile fileZip, string notebookPath)
        {
            if (fileZip == null || fileZip.Length == 0)
                return Json(new { success = false, message = "El archivo ZIP está vacío o no se envió." });

            if (string.IsNullOrWhiteSpace(notebookPath))
                return Json(new { success = false, message = "Ruta del notebook no especificada." });

            try
            {
                string baseName = notebookPath.Split('/').Last();

                using var memoryStream = new MemoryStream();
                fileZip.CopyTo(memoryStream);
                memoryStream.Position = 0;

                using var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read);

                var entry = archive.Entries.FirstOrDefault(e =>
                    e.FullName.EndsWith(baseName + ".py", StringComparison.OrdinalIgnoreCase) ||
                    e.FullName.EndsWith(baseName + ".sql", StringComparison.OrdinalIgnoreCase) ||
                    e.FullName.EndsWith(baseName + ".ipynb", StringComparison.OrdinalIgnoreCase));

                if (entry == null)
                    return Json(new { success = false, message = $"No se encontró el notebook '{baseName}' (.py, .sql o .ipynb) en el ZIP." });

                bool isIpynb = entry.FullName.EndsWith(".ipynb", StringComparison.OrdinalIgnoreCase);

                using var entryStream = entry.Open();
                using var reader = new StreamReader(entryStream);
                string contenido = reader.ReadToEnd();

                var celdasAnalizadas = AnalizarLinajePorCeldas(contenido, isIpynb);

                return Json(new { success = true, celdas = celdasAnalizadas, fileName = entry.Name });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al procesar el ZIP: " + ex.Message });
            }
        }

        private List<CeldaLinajeDto> AnalizarLinajePorCeldas(string codigoFuente, bool isIpynb)
        {
            var resultado = new List<CeldaLinajeDto>();
            List<string> celdas = new List<string>();

            if (isIpynb)
            {
                try
                {
                    using var jsonDoc = JsonDocument.Parse(codigoFuente);
                    if (jsonDoc.RootElement.TryGetProperty("cells", out var cellsArray))
                    {
                        foreach (var cell in cellsArray.EnumerateArray())
                        {
                            if (cell.TryGetProperty("source", out var sourceProp))
                            {
                                if (sourceProp.ValueKind == JsonValueKind.Array)
                                {
                                    var lines = sourceProp.EnumerateArray().Select(s => s.GetString());
                                    celdas.Add(string.Join("", lines));
                                }
                                else if (sourceProp.ValueKind == JsonValueKind.String)
                                {
                                    celdas.Add(sourceProp.GetString() ?? "");
                                }
                            }
                        }
                    }
                }
                catch
                {
                    celdas.Add(codigoFuente);
                }
            }
            else
            {
                string[] separador = new[] { "# COMMAND ----------" };
                celdas = codigoFuente.Split(separador, StringSplitOptions.None).ToList();
            }

            string target = @"(?!IF\b)(?!NOT\b)(?!EXISTS\b)([\w\.\{\}\$\`]+(?:[""'\+ \t]+[\w\.\{\}\$\`]+)*)";

            string stopForOn = @"(?:\b(?:(?:INNER\s+|LEFT\s+|RIGHT\s+|FULL\s+|CROSS\s+)?JOIN|WHERE|GROUP\s+BY|ORDER\s+BY|UNION|HAVING|LIMIT)\b|(?<!\+\s*)(?:""""""|''')(?!\s*\+)|^\s*(?:df|spark|print|if|for|def|return|dbutils|[a-zA-Z0-9_]+\s*(?:=|\+=))\b|;$)";
            string stopForWhere = @"(?:\b(?:GROUP\s+BY|ORDER\s+BY|HAVING|LIMIT|UNION|EXCEPT|INTERSECT)\b|(?<!\+\s*)(?:""""""|''')(?!\s*\+)|^\s*(?:df|spark|print|if|for|def|return|dbutils|[a-zA-Z0-9_]+\s*(?:=|\+=))\b|;$)";
            string stopForGroupBy = @"(?:\b(?:ORDER\s+BY|HAVING|LIMIT|UNION|EXCEPT|INTERSECT)\b|(?<!\+\s*)(?:""""""|''')(?!\s*\+)|^\s*(?:df|spark|print|if|for|def|return|dbutils|[a-zA-Z0-9_]+\s*(?:=|\+=))\b|;$)";
            string stopForOrderBy = @"(?:\b(?:LIMIT|UNION|EXCEPT|INTERSECT)\b|(?<!\+\s*)(?:""""""|''')(?!\s*\+)|^\s*(?:df|spark|print|if|for|def|return|dbutils|[a-zA-Z0-9_]+\s*(?:=|\+=))\b|;$)";

            var reglasRegex = new Dictionary<string, Regex>
            {
                // DDL y Tablas 
                { "CREATE TABLE", new Regex($@"CREATE\s+(?:OR\s+REPLACE\s+)?TABLE\s+(?:IF\s+NOT\s+EXISTS\s+)?[""'\+ \t]*{target}", RegexOptions.IgnoreCase) },
                { "CREATE VIEW", new Regex($@"CREATE\s+(?:OR\s+REPLACE\s+)?(?:TEMP\s+|TEMPORARY\s+)?(?:GLOBAL\s+TEMP\s+)?VIEW\s+(?:IF\s+NOT\s+EXISTS\s+)?[""'\+ \t]*{target}", RegexOptions.IgnoreCase) },
                { "DROP TABLE", new Regex($@"DROP\s+TABLE\s+(?:IF\s+EXISTS\s+)?[""'\+ \t]*{target}", RegexOptions.IgnoreCase) },
                { "DROP VIEW", new Regex($@"DROP\s+(?:TEMP\s+|TEMPORARY\s+)?VIEW\s+(?:IF\s+EXISTS\s+)?[""'\+ \t]*{target}", RegexOptions.IgnoreCase) },
                { "TRUNCATE", new Regex($@"TRUNCATE\s+TABLE\s+[""'\+ \t]*{target}", RegexOptions.IgnoreCase) },

                // DML
                { "INSERT", new Regex($@"INSERT\s+(?:INTO\s+|OVERWRITE\s+)(?:TABLE\s+)?[""'\+ \t]*{target}", RegexOptions.IgnoreCase) },
                { "MERGE", new Regex($@"MERGE\s+INTO\s+[""'\+ \t]*{target}", RegexOptions.IgnoreCase) },
                { "DELETE", new Regex($@"DELETE\s+FROM\s+[""'\+ \t]*{target}", RegexOptions.IgnoreCase) },
                
                // Lecturas y JOINS 
                { "READ TABLE", new Regex($@"SELECT\s+.*?\s+FROM\s+[""'\+ \t]*{target}", RegexOptions.IgnoreCase | RegexOptions.Singleline) },
                { "JOIN TABLE", new Regex($@"(?:(?:INNER|LEFT|RIGHT|FULL|CROSS)\s+)?JOIN\s+[""'\+ \t]*{target}(?:\s+ON\s+([\s\S]+?(?={stopForOn})))?", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Multiline) },
                
                // Filtros y Agrupaciones 
                { "WHERE FILTER", new Regex($@"\bWHERE\s+([\s\S]+?(?={stopForWhere}))", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Multiline) },
                { "GROUP BY", new Regex($@"\bGROUP\s+BY\s+([\s\S]+?(?={stopForGroupBy}))", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Multiline) },
                { "ORDER BY", new Regex($@"\bORDER\s+BY\s+([\s\S]+?(?={stopForOrderBy}))", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Multiline) },

                // Spark Nativos
                { "SPARK READ", new Regex($@"spark\.read\.(?:table|parquet|csv|json|format)\s*\(\s*[""']?{target}[""']?\s*\)", RegexOptions.IgnoreCase) },
                { "SPARK WRITE", new Regex($@"write\.(?:mode\(.*?\)\.)?(?:saveAsTable|parquet|csv|json)\s*\(\s*[""']?{target}[""']?\s*\)", RegexOptions.IgnoreCase) },
                { "SPARK INSERT", new Regex($@"write\.(?:mode\(.*?\)\.)?insertInto\s*\(\s*[""']?{target}[""']?\s*\)", RegexOptions.IgnoreCase) },

                // Databricks Utils
                { "RUN NOTEBOOK", new Regex(@"dbutils\.notebook\.run\s*\(\s*['""]?([^'"",\s\)]+)['""]?", RegexOptions.IgnoreCase) },
                { "GET PARAMETER", new Regex(@"dbutils\.widgets\.get\s*\(\s*['""]?([^'"")\s]+)['""]?\)", RegexOptions.IgnoreCase) },
                { "DBFS MOVE", new Regex(@"dbutils\.fs\.mv\s*\(\s*([^,]+)", RegexOptions.IgnoreCase) },
                { "DBFS REMOVE", new Regex(@"dbutils\.fs\.rm\s*\(\s*([^,]+)", RegexOptions.IgnoreCase) },
                { "DBFS LIST", new Regex(@"dbutils\.fs\.ls\s*\(\s*([^)]+)\)", RegexOptions.IgnoreCase) },
                
                // Custom & Lists
                { "SAVE RDD", new Regex(@"saveAsTextFile\s*\(\s*([^)]+)\)", RegexOptions.IgnoreCase) },
                { "CUSTOM EXPORT", new Regex(@"(?:exportTabla|exportCTR|guardar_rdd)\s*\(\s*([^,]+)", RegexOptions.IgnoreCase) },
                { "LIST DEFINITION", new Regex(@"([a-zA-Z0-9_]+)\s*=\s*\[", RegexOptions.IgnoreCase) },
                { "FOR LOOP", new Regex(@"for\s+([a-zA-Z0-9_,\s]+)\s+in\s+([a-zA-Z0-9_]+)\s*:", RegexOptions.IgnoreCase) }
            };

            for (int i = 0; i < celdas.Count; i++)
            {
                string codigoCelda = celdas[i].Trim();
                if (string.IsNullOrWhiteSpace(codigoCelda)) continue;

                var operacionesCelda = new List<OperacionDetectadaDto>();

                foreach (var regla in reglasRegex)
                {
                    var matches = regla.Value.Matches(codigoCelda);
                    foreach (Match match in matches)
                    {
                        if (regla.Key == "WHERE FILTER" || regla.Key == "GROUP BY" || regla.Key == "ORDER BY")
                        {
                            if (match.Groups.Count > 1 && !string.IsNullOrWhiteSpace(match.Groups[1].Value))
                            {
                                string cond = match.Groups[1].Value;
                                cond = Regex.Replace(cond, @"\s+", " ").Trim();
                                cond = Regex.Replace(cond, @"(?<!\+\s*)""""""$", "");
                                cond = Regex.Replace(cond, @"(?<!\+\s*)'''$", "");
                                cond = Regex.Replace(cond, @"(?<!\+\s*)[""']$", "");

                                if (cond.Length > 2)
                                {
                                    operacionesCelda.Add(new OperacionDetectadaDto
                                    {
                                        TipoOperacion = regla.Key,
                                        Objetivo = cond
                                    });
                                }
                            }
                            continue;
                        }

                        if (match.Groups.Count > 1)
                        {
                            string objetivo = match.Groups[1].Value.Trim().Trim('\'', '"', '+', ' ');

                            if (regla.Key == "READ TABLE")
                            {
                                if (objetivo.ToLower() == "pyspark" || objetivo.ToLower() == "datetime" || objetivo.ToLower() == "dateutil") continue;
                                if (codigoCelda.Contains($"from {objetivo} import", StringComparison.OrdinalIgnoreCase)) continue;
                            }

                            if (objetivo.Length < 2) continue;

                            if (regla.Key == "JOIN TABLE" && match.Groups.Count > 2 && !string.IsNullOrWhiteSpace(match.Groups[2].Value))
                            {
                                string onCond = match.Groups[2].Value;
                                onCond = Regex.Replace(onCond, @"\s+", " ").Trim();
                                onCond = Regex.Replace(onCond, @"(?<!\+\s*)""""""$", "");
                                onCond = Regex.Replace(onCond, @"(?<!\+\s*)'''$", "");
                                onCond = Regex.Replace(onCond, @"(?<!\+\s*)[""']$", "");

                                objetivo += $"\n🔗(ON: {onCond})";
                            }

                            if (regla.Key == "LIST DEFINITION")
                            {
                                int startIndex = match.Index + match.Length - 1;
                                string contentPreview = codigoCelda.Substring(startIndex);
                                if (contentPreview.Length > 200) contentPreview = contentPreview.Substring(0, 200) + "...]";
                                objetivo += $"\n{contentPreview}";
                            }
                            else if (regla.Key == "FOR LOOP")
                            {
                                string iterator = match.Groups[1].Value.Trim();
                                string listName = match.Groups[2].Value.Trim();
                                objetivo = $"{listName}\nIterado como variable: '{iterator}'";
                            }
                            else if (regla.Key == "CREATE TABLE" || regla.Key == "CREATE VIEW" || regla.Key == "DROP TABLE")
                            {
                                var locMatch = Regex.Match(codigoCelda, $@"LOCATION\s+[""'\+ \t]*{target}", RegexOptions.IgnoreCase);
                                if (locMatch.Success)
                                {
                                    string locationLimpia = locMatch.Groups[1].Value.Trim().Trim('\'', '"', '+', ' ');
                                    objetivo += $"\n📍(Location: {locationLimpia})";
                                }
                            }

                            operacionesCelda.Add(new OperacionDetectadaDto
                            {
                                TipoOperacion = regla.Key,
                                Objetivo = objetivo
                            });
                        }
                    }
                }

                if (operacionesCelda.Any())
                {
                    resultado.Add(new CeldaLinajeDto
                    {
                        NumeroCelda = i + 1,
                        Operaciones = operacionesCelda.DistinctBy(o => o.TipoOperacion + o.Objetivo).ToList(),
                        // NUEVO: Enviamos el código crudo respetando todo su formato original
                        CodigoFuenteCrudo = celdas[i]
                    });
                }
            }

            return resultado;
        }
    }

    public class CeldaLinajeDto
    {
        public int NumeroCelda { get; set; }
        public List<OperacionDetectadaDto> Operaciones { get; set; } = new();
        public string CodigoFuenteCrudo { get; set; } = string.Empty;
    }

    public class OperacionDetectadaDto
    {
        public string TipoOperacion { get; set; } = string.Empty;
        public string Objetivo { get; set; } = string.Empty;
    }
}
