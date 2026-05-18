using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NotebookValidator.Web.Services
{
    public class DocGenService
    {
        private static readonly Regex WidgetRegex = new Regex(
            "dbutils\\.widgets\\.(?:text|dropdown|combobox|multiselect)\\(\\s*['\"]([^'\"]+)['\"]\\s*,\\s*['\"]([^'\"]*)['\"]\\s*(?:,\\s*['\"]([^'\"]*)['\"])?",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex CrudOpRegex = new Regex(
            "\\b(CREATE\\s+TABLE|CREATE\\s+VIEW|DROP\\s+TABLE(?:\\s+IF\\s+EXISTS)?|INSERT\\s+INTO|UPDATE|DELETE\\s+FROM|MERGE\\s+INTO)\\s+([A-Za-z0-9_\\.]+)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // NUEVOS REGEX: Para captura de librerías y vistas temporales
        private static readonly Regex ImportRegex = new Regex(
            @"^[ \t]*(?:from|import)\s+([A-Za-z0-9_\.]+)",
            RegexOptions.Multiline | RegexOptions.Compiled);

        private static readonly Regex TempViewPyRegex = new Regex(
            @"createOrReplaceTempView\s*\(\s*['""]([^'""]+)['""]\s*\)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex TempViewSqlRegex = new Regex(
            @"\bCREATE\s+(?:OR\s+REPLACE\s+)?(?:TEMP(?:ORARY)?\s+)?VIEW\s+([A-Za-z0-9_\.]+)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private class SqlOperationDetail
        {
            public string Location { get; set; } = "";
            public string Operation { get; set; } = "";
            public string Target { get; set; } = "";
            public List<string> Sources { get; set; } = new List<string>();
            public string Fields { get; set; } = "No Aplica (*)";
        }

        public async Task<(string Markdown, string Html)> GenerateDocumentationPairAsync(IFormFile file)
        {
            try
            {
                string extension = Path.GetExtension(file.FileName).ToLower();
                string rawContent;

                using (var reader = new StreamReader(file.OpenReadStream()))
                {
                    rawContent = await reader.ReadToEndAsync();
                }

                var widgets = new List<(string Name, string Default, string Label)>();
                var headers = new List<string>();
                var operations = new List<SqlOperationDetail>();

                var globalTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var globalSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // Nuevas colecciones para las capacidades agregadas
                var libraries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var tempViews = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                ExtractMetadataAndWidgets(rawContent, extension, widgets, headers, libraries, tempViews);

                string combinedCode = GetCombinedCodeContent(rawContent, extension);
                string clearSqlCode = NormalizeSqlCode(combinedCode);

                ExtractAdvancedSqlMetrics(clearSqlCode, operations, globalTargets, globalSources, tempViews);

                string markdown = BuildMarkdownString(file.FileName, widgets, headers, operations, globalTargets, globalSources, libraries, tempViews);
                string html = BuildHtmlString(file.FileName, widgets, headers, operations, globalTargets, globalSources, libraries, tempViews);

                return (markdown, html);
            }
            catch (Exception ex)
            {
                string errorMd = $"# ❌ Error Interno al procesar: {file.FileName}\n\nOcurrió un fallo en el motor de lectura.\n\n**Detalle técnico para depuración:**\n```text\n{ex.Message}\n{ex.StackTrace}\n```";
                string errorHtml = $"<h1>❌ Error Interno al procesar: {file.FileName}</h1><p>Ocurrió un fallo en el motor de lectura.</p><pre>{ex.Message}\n{ex.StackTrace}</pre>";
                return (errorMd, errorHtml);
            }
        }

        private string GetCombinedCodeContent(string rawContent, string extension)
        {
            var sb = new StringBuilder();
            if (extension == ".py")
            {
                var lines = rawContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                foreach (var line in lines)
                {
                    if (line.Contains("COMMAND") || line.Trim().StartsWith("#")) continue;
                    sb.AppendLine(line);
                }
            }
            else if (extension == ".ipynb")
            {
                try
                {
                    using (JsonDocument doc = JsonDocument.Parse(rawContent))
                    {
                        var root = doc.RootElement;
                        if (root.TryGetProperty("cells", out var cells) && cells.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var cell in cells.EnumerateArray())
                            {
                                if (cell.TryGetProperty("cell_type", out var typeProp) && typeProp.GetString() == "code")
                                {
                                    if (cell.TryGetProperty("source", out var codeSource))
                                    {
                                        sb.AppendLine(string.Join("", codeSource.EnumerateArray().Select(x => x.GetString())));
                                    }
                                }
                            }
                        }
                    }
                }
                catch { return rawContent; }
            }
            return sb.ToString();
        }

        private string NormalizeSqlCode(string rawCode)
        {
            if (string.IsNullOrEmpty(rawCode)) return "";
            string clean = rawCode.Replace("\"\"\"", "").Replace("'''", "").Replace("\"", "").Replace("'", "");
            clean = clean.Replace("+", " ");
            clean = Regex.Replace(clean, @"\s+", " ");
            clean = clean.Replace(" .", ".").Replace(". ", ".").Replace(" . ", ".");
            return clean;
        }

        private void ExtractMetadataAndWidgets(string rawContent, string extension, List<(string Name, string Default, string Label)> widgets, List<string> headers, HashSet<string> libraries, HashSet<string> tempViews)
        {
            var lines = rawContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            // Extracción multilínea para librerías en ambos formatos
            var importMatches = ImportRegex.Matches(rawContent);
            foreach (Match m in importMatches)
            {
                libraries.Add(m.Groups[1].Value);
            }

            if (extension == ".py")
            {
                foreach (var line in lines)
                {
                    string trimmed = line.Trim();
                    if (trimmed.Contains("COMMAND")) continue;

                    if (trimmed.StartsWith("# MAGIC "))
                    {
                        string magicContent = trimmed.Replace("# MAGIC ", "").Trim();
                        if (magicContent.StartsWith("%md")) magicContent = magicContent.Replace("%md", "").Trim();
                        if (magicContent.StartsWith("#")) headers.Add(magicContent);
                    }
                    else if (trimmed.StartsWith("##") && !trimmed.Contains("---"))
                    {
                        headers.Add(trimmed);
                    }

                    var widgetMatches = WidgetRegex.Matches(line);
                    foreach (Match match in widgetMatches)
                    {
                        string name = match.Groups[1].Value;
                        string defVal = match.Groups[2].Value;
                        string label = match.Groups.Count > 3 ? match.Groups[3].Value : name;
                        if (!widgets.Any(w => w.Name == name)) widgets.Add((name, defVal, string.IsNullOrEmpty(label) ? name : label));
                    }

                    // Captura de Temp Views en estilo PySpark (df.createOrReplaceTempView)
                    var tempViewMatch = TempViewPyRegex.Match(line);
                    if (tempViewMatch.Success) tempViews.Add(tempViewMatch.Groups[1].Value);
                }
            }
        }

        private string FormatFields(string rawFields, bool isUpdate)
        {
            var fieldsList = new List<string>();
            if (!isUpdate)
            {
                var aliasMatches = Regex.Matches(rawFields, @"\bAS\s+(?!integer\b|int\b|string\b|varchar\b|date\b|timestamp\b|decimal\b|double\b|float\b|boolean\b)([A-Za-z0-9_]+)", RegexOptions.IgnoreCase);
                if (aliasMatches.Count > 0)
                {
                    foreach (Match m in aliasMatches) fieldsList.Add(m.Groups[1].Value);
                    return string.Join("<br>• ", fieldsList.Distinct());
                }
            }
            else
            {
                var setMatches = Regex.Matches(rawFields, @"\b([A-Za-z0-9_]+)\s*=", RegexOptions.IgnoreCase);
                if (setMatches.Count > 0)
                {
                    foreach (Match m in setMatches) fieldsList.Add(m.Groups[1].Value);
                    return string.Join("<br>• ", fieldsList.Distinct());
                }
            }

            string cleanRaw = Regex.Replace(rawFields, @"\s+", " ").Trim();
            var parts = cleanRaw.Split(',').Select(p => p.Trim());
            return string.Join(",<br>• ", parts);
        }

        private void ExtractAdvancedSqlMetrics(string cleanSqlCode, List<SqlOperationDetail> operationsList, HashSet<string> globalTargets, HashSet<string> globalSources, HashSet<string> tempViews)
        {
            // Captura de Temp Views en estilo SQL puro
            var sqlTempViewMatches = TempViewSqlRegex.Matches(cleanSqlCode);
            foreach (Match m in sqlTempViewMatches)
            {
                tempViews.Add(m.Groups[1].Value);
            }

            var opMatches = CrudOpRegex.Matches(cleanSqlCode);
            for (int i = 0; i < opMatches.Count; i++)
            {
                var match = opMatches[i];
                var opDetail = new SqlOperationDetail
                {
                    Location = $"Bloque Operativo {i + 1}",
                    Operation = match.Groups[1].Value.ToUpper().Trim().Replace("  ", " "),
                    Target = match.Groups[2].Value.Trim()
                };

                if (opDetail.Target.Equals("USING", StringComparison.OrdinalIgnoreCase) ||
                    opDetail.Target.Equals("DELTA", StringComparison.OrdinalIgnoreCase) ||
                    opDetail.Target.Equals("PARTITIONED", StringComparison.OrdinalIgnoreCase) ||
                    opDetail.Target.Equals("LOCATION", StringComparison.OrdinalIgnoreCase)) continue;

                if (!opDetail.Operation.Contains("DROP"))
                {
                    globalTargets.Add(opDetail.Target);
                }

                int startIdx = match.Index;
                int endIdx = (i + 1 < opMatches.Count) ? opMatches[i + 1].Index : cleanSqlCode.Length;
                string subQuery = cleanSqlCode.Substring(startIdx, endIdx - startIdx);

                var srcMatches = Regex.Matches(subQuery, @"\b(?:FROM|JOIN)\s+([A-Za-z0-9_\.]+)", RegexOptions.IgnoreCase);
                foreach (Match srcMatch in srcMatches)
                {
                    string srcTable = srcMatch.Groups[1].Value.Trim();
                    if (srcTable.Length >= 3 &&
                        !srcTable.Equals("SELECT", StringComparison.OrdinalIgnoreCase) &&
                        !srcTable.Equals("LEFT", StringComparison.OrdinalIgnoreCase) &&
                        !srcTable.Equals("INNER", StringComparison.OrdinalIgnoreCase) &&
                        !srcTable.Equals("RIGHT", StringComparison.OrdinalIgnoreCase))
                    {
                        opDetail.Sources.Add(srcTable);
                        if (!opDetail.Operation.Contains("DROP"))
                        {
                            globalSources.Add(srcTable);
                        }
                    }
                }

                int selectIdx = subQuery.IndexOf("SELECT", StringComparison.OrdinalIgnoreCase);
                int fromIdx = subQuery.LastIndexOf("FROM", StringComparison.OrdinalIgnoreCase);

                if (selectIdx >= 0 && fromIdx > (selectIdx + 6) && !opDetail.Operation.Contains("DROP"))
                {
                    string fieldsRaw = subQuery.Substring(selectIdx + 6, fromIdx - (selectIdx + 6)).Trim();
                    opDetail.Fields = "• " + FormatFields(fieldsRaw, false);
                }
                else if (opDetail.Operation.Contains("UPDATE"))
                {
                    int setIdx = subQuery.IndexOf("SET", StringComparison.OrdinalIgnoreCase);
                    int whereIdx = subQuery.IndexOf("WHERE", StringComparison.OrdinalIgnoreCase);
                    if (setIdx >= 0 && whereIdx > (setIdx + 3))
                    {
                        string fieldsRaw = subQuery.Substring(setIdx + 3, whereIdx - (setIdx + 3)).Trim();
                        opDetail.Fields = "• " + FormatFields(fieldsRaw, true);
                    }
                }

                if (!opDetail.Sources.Any()) opDetail.Sources.Add("Valores Directos / No requiere origen");
                operationsList.Add(opDetail);
            }
        }

        private string BuildMarkdownString(string fileName, List<(string Name, string Default, string Label)> widgets, List<string> headers, List<SqlOperationDetail> operations, HashSet<string> globalTargets, HashSet<string> globalSources, HashSet<string> libraries, HashSet<string> tempViews)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# Ficha Técnica: {Path.GetFileNameWithoutExtension(fileName)}");
            sb.AppendLine($"*Generado de forma automatizada por Bit Solución el {DateTime.Now:dd/MM/yyyy 'a las' HH:mm} hrs.*\n");

            sb.AppendLine("## 📌 Descripción General");
            sb.AppendLine("Escribe aquí una breve descripción del propósito de este componente de datos.\n");

            // SECCIÓN LIBRERÍAS
            sb.AppendLine("## 📦 Librerías y Dependencias Externas");
            if (libraries.Any())
            {
                foreach (var lib in libraries.OrderBy(l => l)) sb.AppendLine($"- `{lib}`");
            }
            else
            {
                sb.AppendLine("*No se detectaron importaciones de librerías externas.*");
            }
            sb.AppendLine();

            sb.AppendLine("## 📊 Tablas Entrada y Salida");

            var inputTables = globalSources.Except(globalTargets).OrderBy(t => t).ToList();
            sb.AppendLine("### Tablas Entrada:");
            if (inputTables.Any())
            {
                foreach (var input in inputTables) sb.AppendLine($"  - `🔍 {input}`");
            }
            else
            {
                sb.AppendLine("  - *No se identificaron tablas de lectura externa puras.*");
            }
            sb.AppendLine();

            var outputTables = globalTargets.Except(globalSources).OrderBy(t => t).ToList();
            sb.AppendLine("### Tablas Salida:");
            if (outputTables.Any())
            {
                foreach (var output in outputTables) sb.AppendLine($"  - `📦 {output}`");
            }
            else
            {
                sb.AppendLine("  - *No se identificaron tablas de escritura final puras.*");
            }
            sb.AppendLine();

            // SECCIÓN TEMP VIEWS
            sb.AppendLine("## ☁️ Estructuras Volátiles en Memoria (Temp Views)");
            if (tempViews.Any())
            {
                foreach (var view in tempViews.OrderBy(v => v)) sb.AppendLine($"- `☄️ {view}`");
            }
            else
            {
                sb.AppendLine("*No se detectó persistencia de Dataframes en memoria (Temp Views).*");
            }
            sb.AppendLine();

            sb.AppendLine("## 🎛️ Widgets y Parámetros del Input");
            if (widgets.Any())
            {
                sb.AppendLine("| Etiqueta Visual | Variable Databricks | Valor por Defecto |");
                sb.AppendLine("| :--- | :--- | :--- |");
                foreach (var widget in widgets)
                {
                    sb.AppendLine($"| {widget.Label} | `{widget.Name}` | *{(string.IsNullOrEmpty(widget.Default) ? "Vacío" : widget.Default)}* |");
                }
            }
            else
            {
                sb.AppendLine("No se detectaron widgets interactivos declarados en este archivo.");
            }
            sb.AppendLine();

            sb.AppendLine("## 🔄 Pipeline de Transformación y Flujo de Datos (CRUD & Linaje)");
            if (operations.Any())
            {
                sb.AppendLine("| Ubicación | Operación | Tabla Destino | Tablas Origen | Campos / Atributos Comprometidos |");
                sb.AppendLine("| :--- | :--- | :--- | :--- | :--- |");
                foreach (var op in operations)
                {
                    string sourcesList = string.Join("<br>", op.Sources.Select(s => $"`{s}`"));
                    sb.AppendLine($"| {op.Location} | **{op.Operation}** | `{op.Target}` | {sourcesList} | {op.Fields} |");
                }
            }
            else
            {
                sb.AppendLine("No se detectaron comandos imperativos de manipulación de datos (DML/DDL) en los hilos analizados.");
            }
            sb.AppendLine();

            sb.AppendLine("## 🏗️ Estructura de Celdas Funcionales");
            if (headers.Any())
            {
                sb.AppendLine("Estructura jerárquica encontrada en el mapa de desarrollo:");
                foreach (var header in headers)
                {
                    int hashes = header.TakeWhile(c => c == '#').Count();
                    if (hashes == 0) hashes = 1;
                    string cleanHeader = header.TrimStart('#').Trim();

                    int spaces = (hashes - 1) * 4;
                    string prefix = new string(' ', spaces);

                    if (hashes == 1 || hashes == 2)
                    {
                        sb.AppendLine($"{prefix}- **{cleanHeader}**");
                    }
                    else
                    {
                        sb.AppendLine($"{prefix}- {cleanHeader}");
                    }
                }
            }
            return sb.ToString();
        }

        private string BuildHtmlString(string fileName, List<(string Name, string Default, string Label)> widgets, List<string> headers, List<SqlOperationDetail> operations, HashSet<string> globalTargets, HashSet<string> globalSources, HashSet<string> libraries, HashSet<string> tempViews)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'><style>");
            sb.AppendLine("body { font-family: 'Calibri', 'Arial', sans-serif; color: #333333; line-height: 1.5; padding: 30px; }");
            sb.AppendLine("h1 { color: #1f4e78; font-size: 22pt; border-bottom: 2px solid #1f4e78; padding-bottom: 6px; font-family: 'Poppins', sans-serif; }");
            sb.AppendLine("h2 { color: #2e74b5; font-size: 15pt; margin-top: 25px; border-bottom: 1px solid #d9d9d9; padding-bottom: 4px; }");
            sb.AppendLine("h3 { color: #595959; font-size: 12pt; margin-top: 15px; margin-bottom: 5px; padding-left: 10px; }");
            sb.AppendLine("table { border-collapse: collapse; width: 100%; margin-top: 12px; margin-bottom: 20px; table-layout: fixed; }");
            sb.AppendLine("th, td { border: 1px solid #bfbfbf; padding: 9px; text-align: left; font-size: 10.5pt; word-wrap: break-word; vertical-align: top; }");
            sb.AppendLine("th { background-color: #f2f2f2; font-weight: bold; color: #1f4e78; }");
            sb.AppendLine("ul { margin-top: 6px; padding-left: 35px; }");
            sb.AppendLine("li { font-size: 11pt; margin-bottom: 5px; color: #404040; }");
            sb.AppendLine("code { font-family: 'Consolas', monospace; background-color: #f5f5f5; padding: 2px 4px; border-radius: 3px; font-size: 9.5pt; color: #a31515; }");
            sb.AppendLine(".audit-text { font-style: italic; color: #7f7f7f; font-size: 9.5pt; margin-bottom: 25px; }");
            sb.AppendLine("</style></head><body>");

            sb.AppendLine($"<h1>Ficha Técnica: {Path.GetFileNameWithoutExtension(fileName)}</h1>");
            sb.AppendLine($"<p class='audit-text'>Generado de forma automatizada por Bit Solución el {DateTime.Now:dd/MM/yyyy 'a las' HH:mm} hrs.</p>");

            sb.AppendLine("<h2>📌 Descripción General</h2>");
            sb.AppendLine("<p>Escribe aquí una breve descripción del propósito de este componente de datos.</p>");

            // SECCIÓN LIBRERÍAS
            sb.AppendLine("<h2>📦 Librerías y Dependencias Externas</h2>");
            if (libraries.Any())
            {
                sb.AppendLine("<ul>");
                foreach (var lib in libraries.OrderBy(l => l)) sb.AppendLine($"<li>Paquete requerido: <code>{lib}</code></li>");
                sb.AppendLine("</ul>");
            }
            else
            {
                sb.AppendLine("<p><em>No se detectaron importaciones de librerías externas.</em></p>");
            }

            sb.AppendLine("<h2>📊 Tablas Entrada y Salida</h2>");

            var inputTables = globalSources.Except(globalTargets).OrderBy(t => t).ToList();
            sb.AppendLine("<h3>Tablas Entrada:</h3><ul>");
            if (inputTables.Any())
            {
                foreach (var input in inputTables) sb.AppendLine($"<li>Componente de Lectura: <code>{input}</code></li>");
            }
            else
            {
                sb.AppendLine("<li>*No se identificaron tablas de lectura externa puras.*</li>");
            }
            sb.AppendLine("</ul>");

            var outputTables = globalTargets.Except(globalSources).OrderBy(t => t).ToList();
            sb.AppendLine("<h3>Tablas Salida:</h3><ul>");
            if (outputTables.Any())
            {
                foreach (var output in outputTables) sb.AppendLine($"<li>Componente de Escritura/Destino: <code>{output}</code></li>");
            }
            else
            {
                sb.AppendLine("<li>*No se identificaron tablas de escritura final puras.*</li>");
            }
            sb.AppendLine("</ul>");

            // SECCIÓN TEMP VIEWS
            sb.AppendLine("<h2>☁️ Estructuras Volátiles en Memoria (Temp Views)</h2>");
            if (tempViews.Any())
            {
                sb.AppendLine("<ul>");
                foreach (var view in tempViews.OrderBy(v => v)) sb.AppendLine($"<li>Vista en Memoria: <code>{view}</code></li>");
                sb.AppendLine("</ul>");
            }
            else
            {
                sb.AppendLine("<p><em>No se detectó persistencia de Dataframes en memoria (Temp Views).</em></p>");
            }

            sb.AppendLine("<h2>🎛️ Widgets y Parámetros del Input</h2>");
            if (widgets.Any())
            {
                sb.AppendLine("<table><thead><tr><th style='width:25%;'>Etiqueta Visual</th><th style='width:35%;'>Variable Databricks</th><th style='width:40%;'>Valor por Defecto</th></tr></thead><tbody>");
                foreach (var widget in widgets)
                {
                    string def = string.IsNullOrEmpty(widget.Default) ? "Vacío" : widget.Default;
                    sb.AppendLine($"<tr><td><strong>{widget.Label}</strong></td><td><code>{widget.Name}</code></td><td>{def}</td></tr>");
                }
                sb.AppendLine("</tbody></table>");
            }

            sb.AppendLine("<h2>🔄 Pipeline de Transformación y Flujo de Datos (CRUD & Linaje)</h2>");
            if (operations.Any())
            {
                sb.AppendLine("<table><thead><tr><th style='width:12%;'>Ubicación</th><th style='width:18%;'>Operación</th><th style='width:24%;'>Tabla Destino</th><th style='width:24%;'>Tablas Origen</th><th style='width:22%;'>Campos Comprometidos</th></tr></thead><tbody>");
                foreach (var op in operations)
                {
                    string srcItems = string.Join("<br><br>", op.Sources.Select(s => $"<code>{s}</code>"));
                    sb.AppendLine($"<tr><td>{op.Location}</td><td><strong>{op.Operation}</strong></td><td><code>{op.Target}</code></td><td>{srcItems}</td><td style='font-size:9.5pt; color:#444;'>{op.Fields}</td></tr>");
                }
                sb.AppendLine("</tbody></table>");
            }

            sb.AppendLine("<h2>🏗️ Estructura de Celdas Funcionales</h2>");
            if (headers.Any())
            {
                sb.AppendLine("<div style='margin-top: 10px;'>");
                foreach (var header in headers)
                {
                    int hashes = header.TakeWhile(c => c == '#').Count();
                    if (hashes == 0) hashes = 1;
                    string cleanHeader = header.TrimStart('#').Trim();

                    if (hashes == 1)
                    {
                        sb.AppendLine($"<div style='margin-left: 0px; font-weight: bold; color: #1f4e78; margin-top: 12px; font-size: 11.5pt;'>&#9632; {cleanHeader}</div>");
                    }
                    else if (hashes == 2)
                    {
                        sb.AppendLine($"<div style='margin-left: 20px; font-weight: bold; color: #2e74b5; margin-top: 6px; font-size: 11pt;'>&bull; {cleanHeader}</div>");
                    }
                    else
                    {
                        int margin = 20 + ((hashes - 2) * 20);
                        sb.AppendLine($"<div style='margin-left: {margin}px; color: #404040; margin-top: 4px; font-size: 10.5pt;'>&#9675; {cleanHeader}</div>");
                    }
                }
                sb.AppendLine("</div>");
            }

            sb.AppendLine("</body></html>");
            return sb.ToString();
        }
    }
}
