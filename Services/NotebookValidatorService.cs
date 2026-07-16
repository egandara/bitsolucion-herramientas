using NotebookValidator.Web.Data;
using NotebookValidator.Web.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ClosedXML.Excel;

namespace NotebookValidator.Web.Services
{
    public class NotebookValidatorService
    {
        private readonly ApplicationDbContext _context;
        private static readonly Regex SqlOperationalRegex = new Regex(@"\b(INSERT|UPDATE|DELETE|MERGE|ALTER|CREATE|DROP|TRUNCATE|REPLACE|COPY)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex LocalFunctionRegex = new Regex(@"^def\s+(\w+)\s*\(", RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex SqlSafeVariantsRegex = new Regex(@"def\s+(sql_safe|SqlSafe|Sql_Safe|sqlsafe)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private const string PyCommandSeparator = "# COMMAND ----------";

        public NotebookValidatorService(ApplicationDbContext context)
        {
            _context = context;
        }

        // ===================================================================================
        // GENERACIÓN DE ARCHIVOS CORREGIDOS: Soporta la reconstrucción estructurada de ZIPs
        // ===================================================================================
        public async Task<byte[]> GenerateCorrectedFilesZipAsync(IEnumerable<(Stream stream, string fileName)> files, List<string> typesToClean)
        {
            using var ms = new MemoryStream();
            using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
            {
                foreach (var file in files)
                {
                    string ext = Path.GetExtension(file.fileName).ToLower();

                    if (ext == ".zip")
                    {
                        // Descomprimir el paquete original para corregir sus componentes internos
                        using (var inputZip = new ZipArchive(file.stream, ZipArchiveMode.Read))
                        {
                            foreach (var entry in inputZip.Entries)
                            {
                                if (entry.Length == 0 || string.IsNullOrEmpty(entry.Name)) continue;

                                string entryExt = Path.GetExtension(entry.Name).ToLower();
                                if (entryExt == ".py" || entryExt == ".ipynb")
                                {
                                    // Omitir manifiestos y metadatos de exportación de Databricks
                                    if (entry.FullName.Contains("manifest.mf", StringComparison.OrdinalIgnoreCase)) continue;

                                    var baseName = Path.GetFileNameWithoutExtension(entry.Name);
                                    string correctedContent;

                                    using (var entryStream = entry.Open())
                                    {
                                        if (entryExt == ".ipynb")
                                        {
                                            var notebook = await JsonSerializer.DeserializeAsync<Notebook>(entryStream);
                                            ApplySmartFix(notebook!, baseName, typesToClean);
                                            correctedContent = JsonSerializer.Serialize(notebook, new JsonSerializerOptions { WriteIndented = true });
                                        }
                                        else
                                        {
                                            using var reader = new StreamReader(entryStream);
                                            var pyContent = await reader.ReadToEndAsync();
                                            correctedContent = ApplySmartFixToPython(pyContent, baseName, typesToClean);
                                        }
                                    }

                                    // Guardar la versión corregida manteniendo la jerarquía de carpetas original del comprimido
                                    var zipEntry = archive.CreateEntry($"📦_[{Path.GetFileNameWithoutExtension(file.fileName)}]_/{entry.FullName}");
                                    using var entryZipStream = zipEntry.Open();
                                    using var writer = new StreamWriter(entryZipStream);
                                    await writer.WriteAsync(correctedContent);
                                }
                            }
                        }
                    }
                    else if (ext == ".py" || ext == ".ipynb")
                    {
                        var baseName = Path.GetFileNameWithoutExtension(file.fileName);
                        string correctedContent;

                        if (file.fileName.EndsWith(".ipynb"))
                        {
                            var notebook = await JsonSerializer.DeserializeAsync<Notebook>(file.stream);
                            ApplySmartFix(notebook!, baseName, typesToClean);
                            correctedContent = JsonSerializer.Serialize(notebook, new JsonSerializerOptions { WriteIndented = true });
                        }
                        else
                        {
                            using var reader = new StreamReader(file.stream);
                            var pyContent = await reader.ReadToEndAsync();
                            correctedContent = ApplySmartFixToPython(pyContent, baseName, typesToClean);
                        }

                        var entry = archive.CreateEntry(file.fileName);
                        using var entryStream = entry.Open();
                        using var writer = new StreamWriter(entryStream);
                        await writer.WriteAsync(correctedContent);
                    }
                }
            }
            return ms.ToArray();
        }

        private void ApplySmartFix(Notebook notebook, string baseName, List<string> typesToClean)
        {
            if (notebook?.Cells == null) return;

            // 1. Corrección Inteligente de Header
            if (typesToClean.Contains("Header Incorrecto") && notebook.Cells.Any())
            {
                var firstCellStr = string.Join("\n", notebook.Cells[0].Source);
                if (firstCellStr.Contains("# Databricks notebook source"))
                {
                    notebook.Cells[0].Source = new List<string> {
                        "# Databricks notebook source\n",
                        "# MAGIC %md\n",
                        $"# MAGIC # {baseName}\n"
                    };
                }
                else
                {
                    notebook.Cells[0].Source = new List<string> { $"# {baseName}\n" };
                }
            }

            // 2. Corrección de truncado de Footer usando GetCleanedCellText
            if (typesToClean.Contains("Código después de Mensaje Final"))
            {
                int exitIndex = -1;
                for (int i = 0; i < notebook.Cells.Count - 1; i++)
                {
                    var currentCleaned = GetCleanedCellText(notebook.Cells[i].Source);
                    var nextSource = string.Join("\n", notebook.Cells[i + 1].Source).Trim();

                    if ((currentCleaned == "# Mensaje Final" || currentCleaned == "## Mensaje Final") && nextSource.Contains("dbutils.notebook.exit"))
                    {
                        exitIndex = i + 1;
                        break;
                    }
                }
                if (exitIndex != -1 && exitIndex < notebook.Cells.Count - 1)
                {
                    notebook.Cells = notebook.Cells.Take(exitIndex + 1).ToList();
                }
            }

            bool fixSqlSafe = typesToClean.Contains("Definición local de sqlsafe");
            bool fixLocalFunc = typesToClean.Contains("Función declarada localmente");

            if (fixSqlSafe)
            {
                foreach (var cell in notebook.Cells.Where(c => c.CellType == "code"))
                {
                    for (int j = 0; j < cell.Source.Count; j++)
                    {
                        string original = cell.Source[j];
                        string updated = Regex.Replace(original, @"\b(sql_safe|SqlSafe|Sql_Safe|SQLSafe)\b", "sqlsafe");
                        if (original != updated) cell.Source[j] = updated;
                    }
                }
            }

            var unusedVars = GetUnusedVariables(notebook.Cells);
            var unusedImports = GetUnusedImportsList(notebook.Cells);

            notebook.Cells = notebook.Cells.Where(cell => {
                if (cell.CellType != "code") return true;

                var lines = cell.Source.Select(l => l.TrimEnd('\r', '\n')).ToList();
                var newLines = new List<string>();
                bool modified = false;

                for (int i = 0; i < lines.Count; i++)
                {
                    var line = lines[i];
                    var trimmed = line.Trim();

                    if (string.IsNullOrWhiteSpace(trimmed)) { newLines.Add(line); continue; }

                    var funcMatch = Regex.Match(line, @"^([ \t]*)def\s+(\w+)\s*\(");
                    if (funcMatch.Success)
                    {
                        string funcName = funcMatch.Groups[2].Value;
                        bool isSqlSafe = Regex.IsMatch(funcName, @"^(sql_safe|SqlSafe|Sql_Safe|sqlsafe)$", RegexOptions.IgnoreCase);

                        if ((fixSqlSafe && isSqlSafe) || (fixLocalFunc && !isSqlSafe))
                        {
                            modified = true;
                            int baseIndent = funcMatch.Groups[1].Value.Length;

                            while (i + 1 < lines.Count)
                            {
                                string nextLine = lines[i + 1];
                                if (string.IsNullOrWhiteSpace(nextLine)) { i++; continue; }
                                int currentIndent = nextLine.TakeWhile(c => c == ' ' || c == '\t').Count();

                                if (currentIndent <= baseIndent && !nextLine.TrimStart().StartsWith("#"))
                                    break;

                                i++;
                            }
                            continue;
                        }
                    }

                    if ((typesToClean.Contains("Código SQL en Databricks") || typesToClean.Contains("SQL: Informativo (SELECT)") || typesToClean.Contains("SQL: Vacío")) && trimmed.StartsWith("%sql") && !SqlOperationalRegex.IsMatch(trimmed))
                    { modified = true; continue; }

                    if (typesToClean.Contains("Footer Incorrecto") && trimmed.Contains("dbutils.notebook.exit"))
                    { modified = true; continue; }

                    bool shouldRemove = false;
                    if (typesToClean.Contains("Variable de Widget no usada"))
                    {
                        foreach (var uv in unusedVars)
                        {
                            if (Regex.IsMatch(trimmed, $@"^{uv}\s*=")) { shouldRemove = true; break; }
                            if (trimmed.StartsWith("print") && trimmed.Contains(uv) && !trimmed.Contains("Parámetros")) { shouldRemove = true; break; }
                        }
                    }
                    if (!shouldRemove && typesToClean.Contains("Import no usado"))
                    {
                        foreach (var ui in unusedImports)
                        {
                            if (Regex.IsMatch(trimmed, $@"\b(import|from)\b.*\b{ui}\b")) { shouldRemove = true; break; }
                        }
                    }

                    if (shouldRemove) { modified = true; }
                    else { newLines.Add(line); }
                }

                if (modified || newLines.Count != lines.Count)
                {
                    cell.Source = newLines.Select(l => l + "\n").ToList();
                }

                return cell.Source.Any(l => !string.IsNullOrWhiteSpace(l));
            }).ToList();

            // 3. Corrección Inteligente de Footer
            if (typesToClean.Contains("Footer Incorrecto"))
            {
                bool isPyFormat = notebook.Cells.Any(c => string.Join("\n", c.Source).Contains("# Databricks notebook source") || string.Join("\n", c.Source).Contains("# MAGIC"));

                if (isPyFormat)
                {
                    notebook.Cells.Add(new Cell { CellType = "code", Source = new List<string> { "# MAGIC %md\n", "# MAGIC ## Mensaje Final\n" } });
                }
                else
                {
                    notebook.Cells.Add(new Cell { CellType = "code", Source = new List<string> { "## Mensaje Final\n" } });
                }

                notebook.Cells.Add(new Cell { CellType = "code", Source = new List<string> { "dbutils.notebook.exit(\"{\\\"coderror\\\":\\\"0\\\", \\\"msgerror\\\":\\\"Notebook termina ejecucion satisfactoriamente\\\"}\")\n" } });
            }
        }

        private string ApplySmartFixToPython(string content, string baseName, List<string> typesToClean)
        {
            var blocks = Regex.Split(content, @"^#\s*COMMAND\s*-+", RegexOptions.Multiline).ToList();
            var cells = blocks.Select(b => new Cell { CellType = "code", Source = b.Split('\n').ToList() }).ToList();
            var nb = new Notebook { Cells = cells };
            ApplySmartFix(nb, baseName, typesToClean);
            return string.Join("\n" + PyCommandSeparator + "\n", nb.Cells.Select(c => string.Join("\n", c.Source)));
        }

        // ===================================================================================
        // PROCESAMIENTO CENTRAL: Apertura iterativa y extracción semántica de archivos ZIP
        // ===================================================================================
        public async Task<(List<Finding> allFindings, int filesCount, Dictionary<string, List<string>> fileVariables)> ProcessFilesAsync(IEnumerable<(Stream stream, string fileName)> files)
        {
            var notebooksToProcess = new List<(string tempPath, string originalName)>();
            var tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDirectory);
            var allFindings = new List<Finding>();
            var fileVariables = new Dictionary<string, List<string>>();

            try
            {
                foreach (var file in files)
                {
                    string ext = Path.GetExtension(file.fileName).ToLower();

                    if (ext == ".zip")
                    {
                        // Descomprimir de forma recursiva mapeando rutas jerárquicas internas
                        using (var archive = new ZipArchive(file.stream, ZipArchiveMode.Read))
                        {
                            foreach (var entry in archive.Entries)
                            {
                                if (entry.Length == 0 || string.IsNullOrEmpty(entry.Name)) continue;

                                string entryExt = Path.GetExtension(entry.Name).ToLower();
                                if (entryExt == ".py" || entryExt == ".ipynb")
                                {
                                    if (entry.FullName.Contains("manifest.mf", StringComparison.OrdinalIgnoreCase)) continue;

                                    string entryTempKey = Guid.NewGuid().ToString("N") + entryExt;
                                    string tempFilePath = Path.Combine(tempDirectory, entryTempKey);

                                    using (var entryStream = entry.Open())
                                    using (var fs = new FileStream(tempFilePath, FileMode.Create))
                                    {
                                        await entryStream.CopyToAsync(fs);
                                    }

                                    // Formato Solicitado: Agrega el prefijo visual de pertenencia al paquete
                                    string displayName = $"📦 [{file.fileName}] /{entry.FullName}";
                                    notebooksToProcess.Add((tempFilePath, displayName));
                                }
                            }
                        }
                    }
                    else if (ext == ".py" || ext == ".ipynb")
                    {
                        var tempFilePath = Path.Combine(tempDirectory, file.fileName);
                        using (var fs = new FileStream(tempFilePath, FileMode.Create)) { await file.stream.CopyToAsync(fs); }
                        notebooksToProcess.Add((tempFilePath, file.fileName));
                    }
                }

                foreach (var (tempPath, originalName) in notebooksToProcess)
                {
                    var (findings, variables) = await AnalyzeDetailedAsync(tempPath, originalName);
                    allFindings.AddRange(findings);
                    fileVariables[originalName] = variables;
                }
                return (allFindings, notebooksToProcess.Count, fileVariables);
            }
            finally { if (Directory.Exists(tempDirectory)) Directory.Delete(tempDirectory, true); }
        }

        private string GetCleanedCellText(IEnumerable<string> sourceLines)
        {
            var cleaned = sourceLines
                .Select(l => l.Trim('\r', '\n', ' '))
                .Where(l => !string.IsNullOrWhiteSpace(l) &&
                            !l.Equals("# Databricks notebook source", StringComparison.OrdinalIgnoreCase) &&
                            !l.Equals("# MAGIC %md", StringComparison.OrdinalIgnoreCase))
                .Select(l => {
                    if (l.StartsWith("# MAGIC ", StringComparison.OrdinalIgnoreCase)) return l.Substring(8).TrimStart();
                    if (l.StartsWith("# MAGIC", StringComparison.OrdinalIgnoreCase)) return l.Substring(7).TrimStart();
                    return l;
                })
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToList();

            return string.Join("\n", cleaned).Trim();
        }

        private async Task<(List<Finding> findings, List<string> variables)> AnalyzeDetailedAsync(string filePath, string originalFileName)
        {
            var findings = new List<Finding>();
            var content = await File.ReadAllTextAsync(filePath);
            List<Cell> cells;

            if (filePath.EndsWith(".ipynb"))
            {
                var nb = JsonSerializer.Deserialize<Notebook>(content) ?? new Notebook();
                cells = nb.Cells;
            }
            else
            {
                cells = Regex.Split(content, @"^#\s*COMMAND\s*-+", RegexOptions.Multiline)
                    .Select(c => new Cell { CellType = "code", Source = c.Split('\n').ToList() }).ToList();
            }

            findings.AddRange(ValidateHeader(cells, originalFileName));
            findings.AddRange(ValidateFooter(cells, originalFileName));

            var widgetVars = new List<string>();
            var widgetRegex = new Regex(@"^(\w+)\s*=\s*dbutils\.widgets\.get", RegexOptions.Multiline);
            foreach (var cell in cells.Where(c => c.CellType == "code"))
            {
                foreach (var line in cell.Source)
                {
                    var match = widgetRegex.Match(line.Trim());
                    if (match.Success) widgetVars.Add(match.Groups[1].Value);
                }
            }

            findings.AddRange(DetectUnusedElementsWithLocation(cells, originalFileName, widgetVars));

            var rules = await _context.ValidationRules.Where(r => r.IsEnabled).ToListAsync();
            var mixedLangRegex = new Regex(@"^%(scala|r|sh)\b", RegexOptions.Multiline);

            for (int i = 0; i < cells.Count; i++)
            {
                var cellSource = string.Join("\n", cells[i].Source);

                foreach (Match m in mixedLangRegex.Matches(cellSource))
                {
                    int lineNum = cellSource.Take(m.Index).Count(c => c == '\n') + 1;
                    findings.Add(new Finding(originalFileName, "Uso de lenguaje mixto", $"Se detectó el uso de '{m.Value}'. Se recomienda estandarizar en un solo lenguaje (PySpark).", i + 1, lineNum, m.Value, cellSource, "Warning"));
                }

                var cellLines = cells[i].Source.Select(l => l.TrimEnd('\r', '\n')).ToList();
                for (int l = 0; l < cellLines.Count; l++)
                {
                    var line = cellLines[l];
                    var funcMatch = Regex.Match(line, @"^([ \t]*)def\s+(\w+)\s*\(");

                    if (funcMatch.Success)
                    {
                        string funcName = funcMatch.Groups[2].Value;
                        int baseIndent = funcMatch.Groups[1].Value.Length;

                        var funcBlockLines = new List<string> { line };
                        int endLine = l;

                        for (int next = l + 1; next < cellLines.Count; next++)
                        {
                            string nextLine = cellLines[next];
                            if (string.IsNullOrWhiteSpace(nextLine))
                            {
                                funcBlockLines.Add(nextLine);
                                continue;
                            }

                            int currentIndent = nextLine.TakeWhile(c => c == ' ' || c == '\t').Count();
                            if (currentIndent <= baseIndent && !nextLine.TrimStart().StartsWith("#"))
                                break;

                            funcBlockLines.Add(nextLine);
                            endLine = next;
                        }

                        while (funcBlockLines.Count > 0 && string.IsNullOrWhiteSpace(funcBlockLines.Last()))
                            funcBlockLines.RemoveAt(funcBlockLines.Count - 1);

                        string extractedSource = string.Join("\n", funcBlockLines);
                        int lineNum = l + 1;

                        if (Regex.IsMatch(funcName, @"^(sql_safe|SqlSafe|Sql_Safe|sqlsafe)$", RegexOptions.IgnoreCase))
                        {
                            findings.Add(new Finding(originalFileName, "Definición local de sqlsafe", "Variante local de sqlsafe detectada. Se recomienda eliminarla y usar el estándar del maestro.", i + 1, lineNum, funcName, extractedSource, "Critical"));
                        }
                        else
                        {
                            findings.Add(new Finding(originalFileName, "Función declarada localmente", $"Estás definiendo la función '{funcName}' localmente. Considera agregarla al maestro Funciones.ipynb.", i + 1, lineNum, funcName, extractedSource, "Info"));
                        }

                        l = endLine;
                    }
                }

                foreach (var rule in rules)
                {
                    var matches = Regex.Matches(cellSource, rule.RegexPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                    foreach (Match m in matches)
                    {
                        int lineNum = cellSource.Take(m.Index).Count(c => c == '\n') + 1;
                        string fType = rule.RuleName;
                        string fSev = rule.Severity;
                        string fDet = rule.DetailsMessage;

                        if (fType.Contains("Base", StringComparison.OrdinalIgnoreCase) ||
                            fType.Contains("Datos", StringComparison.OrdinalIgnoreCase) ||
                            fType.Contains("Hardcode", StringComparison.OrdinalIgnoreCase) ||
                            fType.Contains("Esquema", StringComparison.OrdinalIgnoreCase))
                        {
                            if (m.Value.Contains("{") || m.Value.Contains("}")) continue;

                            if (lineNum > 0 && lineNum <= cellLines.Count)
                            {
                                string exactLine = cellLines[lineNum - 1].Trim();

                                if (exactLine.Contains("{") && exactLine.Contains("}")) continue;

                                if ((exactLine.StartsWith("from ", StringComparison.OrdinalIgnoreCase) && exactLine.Contains("import ", StringComparison.OrdinalIgnoreCase)) ||
                                    exactLine.StartsWith("import ", StringComparison.OrdinalIgnoreCase)) continue;
                            }
                        }

                        if (fType.Contains("SQL", StringComparison.OrdinalIgnoreCase))
                        {
                            var sqlMatch = SqlOperationalRegex.Match(cellSource);
                            var isJustSqlEmpty = cellSource.Trim().Equals("%sql", StringComparison.OrdinalIgnoreCase);
                            fSev = "Critical";

                            if (isJustSqlEmpty)
                            {
                                fType = "SQL: Vacío";
                                fDet = "Celda SQL sin consulta. Puede eliminarse automáticamente desde el resumen.";
                            }
                            else if (sqlMatch.Success)
                            {
                                string op = sqlMatch.Groups[1].Value.ToUpper();
                                if (op == "CREATE" || op == "ALTER" || op == "DROP" || op == "TRUNCATE" || op == "REPLACE")
                                    fType = $"SQL: DDL ({op})";
                                else
                                    fType = $"SQL: {op}";
                                fDet = $"Operación {op} detectada. Haz clic en la varita para estandarizar con sqlSafe.";
                            }
                            else
                            {
                                fType = "SQL: Informativo (SELECT)";
                                fDet = "Consulta de lectura pura. Puede eliminarse automáticamente desde el resumen.";
                            }
                        }

                        findings.Add(new Finding(originalFileName, fType, fDet, i + 1, lineNum, m.Value, cellSource, fSev));
                    }
                }
            }
            return (findings, widgetVars.Distinct().ToList());
        }

        private IEnumerable<Finding> DetectUnusedElementsWithLocation(List<Cell> cells, string fileName, List<string> widgets)
        {
            var codeCells = cells.Where(c => c.CellType == "code").ToList();
            var fullCode = string.Join("\n", codeCells.Select(c => string.Join("\n", c.Source)));

            foreach (var v in widgets)
            {
                if (Regex.Matches(fullCode, $@"\b{v}\b").Count <= 2)
                {
                    for (int i = 0; i < cells.Count; i++)
                    {
                        for (int l = 0; l < cells[i].Source.Count; l++)
                        {
                            if (Regex.IsMatch(cells[i].Source[l], $@"^{v}\s*="))
                            {
                                yield return new Finding(fileName, "Variable de Widget no usada", $"La variable '{v}' no se utiliza.", i + 1, l + 1, v, string.Join("\n", cells[i].Source), "Warning");
                            }
                        }
                    }
                }
            }

            var importRegex = new Regex(@"^\s*(?:import\s+(\w+)(?:\s+as\s+(\w+))?|from\s+(\w+)\s+import\s+(\w+)(?:\s+as\s+(\w+))?)", RegexOptions.Multiline);
            foreach (Match m in importRegex.Matches(fullCode))
            {
                string id = !string.IsNullOrEmpty(m.Groups[2].Value) ? m.Groups[2].Value :
                            (!string.IsNullOrEmpty(m.Groups[5].Value) ? m.Groups[5].Value :
                            (!string.IsNullOrEmpty(m.Groups[4].Value) ? m.Groups[4].Value : m.Groups[1].Value));
                if (!string.IsNullOrEmpty(id) && Regex.Matches(fullCode, $@"\b{id}\b").Count <= 1)
                {
                    for (int i = 0; i < cells.Count; i++)
                    {
                        for (int l = 0; l < cells[i].Source.Count; l++)
                        {
                            if (cells[i].Source[l].Contains(m.Value.Trim()))
                            {
                                yield return new Finding(fileName, "Import no usado", $"La librería/función '{id}' no se utiliza.", i + 1, l + 1, id, string.Join("\n", cells[i].Source), "Info");
                            }
                        }
                    }
                }
            }
        }

        private List<string> GetUnusedVariables(List<Cell> cells)
        {
            var widgetVars = new List<string>();
            var widgetRegex = new Regex(@"^(\w+)\s*=\s*dbutils\.widgets\.get", RegexOptions.Multiline);
            var fullCode = string.Join("\n", cells.Where(c => c.CellType == "code").Select(c => string.Join("\n", c.Source)));
            foreach (var cell in cells.Where(c => c.CellType == "code"))
            {
                foreach (var line in cell.Source)
                {
                    var match = widgetRegex.Match(line.Trim());
                    if (match.Success) widgetVars.Add(match.Groups[1].Value);
                }
            }
            return widgetVars.Where(v => Regex.Matches(fullCode, $@"\b{v}\b").Count <= 2).ToList();
        }

        private List<string> GetUnusedImportsList(List<Cell> cells)
        {
            var unused = new List<string>();
            var fullCode = string.Join("\n", cells.Where(c => c.CellType == "code").Select(c => string.Join("\n", c.Source)));
            var importRegex = new Regex(@"^\s*(?:import\s+(\w+)(?:\s+as\s+(\w+))?|from\s+(\w+)\s+import\s+(\w+)(?:\s+as\s+(\w+))?)", RegexOptions.Multiline);
            foreach (Match m in importRegex.Matches(fullCode))
            {
                string id = !string.IsNullOrEmpty(m.Groups[2].Value) ? m.Groups[2].Value :
                            (!string.IsNullOrEmpty(m.Groups[5].Value) ? m.Groups[5].Value :
                            (!string.IsNullOrEmpty(m.Groups[4].Value) ? m.Groups[4].Value : m.Groups[1].Value));
                if (!string.IsNullOrEmpty(id) && Regex.Matches(fullCode, $@"\b{id}\b").Count <= 1) unused.Add(id);
            }
            return unused;
        }

        private IEnumerable<Finding> ValidateHeader(List<Cell> cells, string fileName)
        {
            if (!cells.Any()) yield break;
            var expected = $"# {Path.GetFileNameWithoutExtension(fileName)}";

            var actualCleaned = GetCleanedCellText(cells[0].Source);

            if (!actualCleaned.StartsWith(expected, StringComparison.OrdinalIgnoreCase))
            {
                var actualStr = string.Join("\n", cells[0].Source).Trim();
                yield return new Finding(fileName, "Header Incorrecto", $"Debe ser '{expected}'", 1, 1, actualStr, actualStr, "Warning");
            }
        }

        private IEnumerable<Finding> ValidateFooter(List<Cell> cells, string fileName)
        {
            string targetExit = "dbutils.notebook.exit";
            bool pairFound = false;
            int messagePairIndex = -1;

            for (int i = 0; i < cells.Count - 1; i++)
            {
                var currentCleaned = GetCleanedCellText(cells[i].Source);
                var nextCleaned = string.Join("\n", cells[i + 1].Source).Trim();

                if ((currentCleaned == "# Mensaje Final" || currentCleaned == "## Mensaje Final") && nextCleaned.Contains(targetExit))
                {
                    pairFound = true;
                    messagePairIndex = i;
                    break;
                }
            }

            if (!pairFound)
            {
                yield return new Finding(fileName, "Footer Incorrecto", "Falta el par 'Mensaje Final' + exit.", cells.Count, 1, "Cierre faltante", "", "Info");
            }
            else if (messagePairIndex + 2 < cells.Count)
            {
                bool hasRealCode = false;
                for (int k = messagePairIndex + 2; k < cells.Count; k++)
                {
                    var content = string.Join("\n", cells[k].Source).Trim();
                    if (!string.IsNullOrWhiteSpace(content)) { hasRealCode = true; break; }
                }

                if (hasRealCode)
                {
                    yield return new Finding(fileName, "Código después de Mensaje Final", "Hay celdas inútiles después de la salida.", messagePairIndex + 3, 1, "Código muerto", "", "Warning");
                }
            }
        }

        public byte[] GenerateExcelReportBytes(List<Finding> findings, AnalysisRun runInfo, string? logoPath = null)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Reporte");
            if (File.Exists(logoPath)) ws.AddPicture(logoPath).MoveTo(ws.Cell("A1")).Scale(0.4);
            ws.Cell("C2").Value = "REPORTE BITSOLUCIÓN";
            var data = findings.Select(f => new { f.FileName, f.FindingType, f.Severity, Ubicacion = $"Celda {f.CellNumber}, Lín {f.LineNumber}", f.Details }).ToList();
            ws.Cell(8, 1).InsertTable(data).Theme = XLTableTheme.TableStyleMedium9;
            ws.Columns().AdjustToContents();
            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }

        // ===================================================================================
        // MOTOR DE ESTIMACIÓN DE ESFUERZO AVANZADO (FASES MACRO Y CUSTOM)
        // ===================================================================================
        public byte[] GenerateCotizacionExcelBytes(List<Finding> findings, AnalysisRun runInfo, string logoPath, CotizacionRequest req)
        {
            var configReglas = new Dictionary<string, (double Costo, bool EsGlobal)>
            {
                { "Celdas con Print", (0.1, true) },
                { "Import no usado", (0.1, true) },
                { "Variable de Widget no usada", (0.1, true) },
                { "Header Incorrecto", (0.1, true) },
                { "Footer Incorrecto", (0.1, true) },
                { "Código después de Mensaje Final", (0.1, true) },
                { "Uso de lenguaje mixto", (0.5, true) },
                { "SQL:", (1.0, false) },
                { "SELECT *", (0.5, false) },
                { "Bases de Datos Hardcodeadas", (1.0, false) },
                { "Definición local de sqlsafe", (1.0, false) },
                { "Función declarada localmente", (0.5, false) }
            };

            var hallazgosConCosto = new List<dynamic>();
            var cobrosGlobalesAplicados = new HashSet<string>();

            foreach (var f in findings)
            {
                var matchedKey = configReglas.Keys.FirstOrDefault(k => f.FindingType.StartsWith(k, StringComparison.OrdinalIgnoreCase) || f.FindingType.Contains(k));
                var config = matchedKey != null ? configReglas[matchedKey] : (Costo: 0.2, EsGlobal: false);
                double costoAplicado = config.Costo;

                if (config.EsGlobal)
                {
                    string trackingKey = $"{f.FileName}_{f.FindingType}";
                    if (cobrosGlobalesAplicados.Contains(trackingKey)) costoAplicado = 0;
                    else cobrosGlobalesAplicados.Add(trackingKey);
                }

                hallazgosConCosto.Add(new { Original = f, CostoAplicado = costoAplicado, ModoCobro = config.EsGlobal ? "Por Notebook" : "Por Ocurrencia" });
            }

            // MATEMÁTICA DE LAS 4 FASES
            double horasRefactorBase = hallazgosConCosto.Sum(x => (double)x.CostoAplicado);
            double horasRefactor = horasRefactorBase * req.Multiplicador;

            double horasOrqJobs = req.CantidadJobs * 2.0;
            double horasOrqCicd = req.IncluirCicd && req.CantidadJobs > 0 ? (req.CantidadJobs * 1.0) + 4.0 : 0;
            double horasOrquestacion = horasOrqJobs + horasOrqCicd;

            double horasQa = req.CantidadTablas * 3.0;
            double horasCustom = req.CustomTasks?.Sum(t => t.Horas) ?? 0;

            double horasTotalProyecto = horasRefactor + horasOrquestacion + horasQa + horasCustom;
            double diasHabilesEstimados = horasTotalProyecto / 6.0; // Asumiendo 6 horas productivas/día

            using var workbook = new XLWorkbook();

            // ─────────────────────────────────────────────────────────────
            // HOJA 1: RESUMEN EJECUTIVO (EL PRESUPUESTO)
            // ─────────────────────────────────────────────────────────────
            var ws1 = workbook.Worksheets.Add("Resumen Ejecutivo");
            ws1.ShowGridLines = false;

            // Cabecera oscura corporativa ampliada
            ws1.Range("A1:J4").Style.Fill.SetBackgroundColor(XLColor.FromArgb(11, 13, 22));
            if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath)) ws1.AddPicture(logoPath).MoveTo(ws1.Cell("B2")).Scale(0.45);

            ws1.Cell("D3").Value = "PRESUPUESTO ESTIMADO DE MIGRACIÓN (UNITY CATALOG)";
            ws1.Range("D3:J3").Merge().Style.Font.SetBold().Font.SetFontSize(15).Font.SetFontColor(XLColor.White).Alignment.SetVertical(XLAlignmentVerticalValues.Center);

            // ── BLOQUE IZQUIERDO: PARÁMETROS BASE ──
            ws1.Cell("B6").Value = "PARÁMETROS DEL PROYECTO";
            ws1.Range("B6:C6").Merge().Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromArgb(13, 202, 240)).Font.SetFontColor(XLColor.Black);

            ws1.Cell("B7").Value = "Total Notebooks Analizados:"; ws1.Cell("C7").Value = findings.Select(f => f.FileName).Distinct().Count();
            ws1.Cell("B8").Value = "Complejidad de Refactorización:"; ws1.Cell("C8").Value = $"{req.DificultadLabel} ({req.Multiplicador}x)";
            ws1.Cell("B9").Value = "Jobs a Orquestar:"; ws1.Cell("C9").Value = req.CantidadJobs;
            ws1.Cell("B10").Value = "Tablas a Cuadrar (QA):"; ws1.Cell("C10").Value = req.CantidadTablas;

            ws1.Cell("B11").Value = "Duración Estimada (Días):"; ws1.Cell("C11").Value = Math.Round(diasHabilesEstimados, 1);
            ws1.Cell("C11").Style.Font.SetBold().Font.SetFontColor(XLColor.SeaGreen);

            ws1.Range("B7:B11").Style.Font.SetBold();
            ws1.Range("B6:C11").Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin);

            // ── BLOQUE CENTRAL: DESGLOSE DE ESFUERZO (CON BARRAS DE DATOS) ──
            ws1.Cell("E6").Value = "DESGLOSE DE ESFUERZO (Horas)";
            ws1.Range("E6:F6").Merge().Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromArgb(11, 13, 22)).Font.SetFontColor(XLColor.White);

            ws1.Cell("E7").Value = "Fase 1: Refactorización de Código"; ws1.Cell("F7").Value = Math.Round(horasRefactor, 1);
            ws1.Cell("E8").Value = "Fase 2: Orquestación y CI/CD"; ws1.Cell("F8").Value = Math.Round(horasOrquestacion, 1);
            ws1.Cell("E9").Value = "Fase 3: Calidad y Cuadratura (QA)"; ws1.Cell("F9").Value = Math.Round(horasQa, 1);
            ws1.Cell("E10").Value = "Fase 4: Tareas Adicionales"; ws1.Cell("F10").Value = Math.Round(horasCustom, 1);

            ws1.Cell("E11").Value = "ESFUERZO TOTAL DEL PROYECTO:"; ws1.Cell("F11").Value = Math.Round(horasTotalProyecto, 1);

            ws1.Range("E7:F10").Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin);
            ws1.Range("E11:F11").Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromArgb(255, 193, 7)).Border.SetOutsideBorder(XLBorderStyleValues.Medium);

            // Gráfico In-Cell (Barras de Datos) para las fases
            ws1.Range("F7:F10").AddConditionalFormat().DataBar(XLColor.FromArgb(13, 202, 240));

            // ── BLOQUE DERECHO: TOP NOTEBOOKS PROBLEMÁTICOS ──
            var topNotebooks = hallazgosConCosto
                .GroupBy(x => x.Original.FileName)
                .Select(g => new {
                    Notebook = (string)g.Key,
                    Horas = Math.Round(g.Sum(x => (double)x.CostoAplicado) * req.Multiplicador, 1)
                })
                .OrderByDescending(x => x.Horas)
                .Take(10)
                .ToList();

            if (topNotebooks.Any())
            {
                ws1.Cell("H6").Value = "TOP NOTEBOOKS MÁS COMPLEJOS";
                ws1.Range("H6:I6").Merge().Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromArgb(220, 53, 69)).Font.SetFontColor(XLColor.White);

                ws1.Cell("H7").Value = "Nombre del Archivo";
                ws1.Cell("I7").Value = "Horas";
                ws1.Range("H7:I7").Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.LightGray);

                int rowTop = 8;
                foreach (var tn in topNotebooks)
                {
                    ws1.Cell(rowTop, 8).Value = tn.Notebook;
                    ws1.Cell(rowTop, 9).Value = tn.Horas;
                    rowTop++;
                }

                ws1.Range($"H7:I{rowTop - 1}").Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin);

                // Gráfico In-Cell (Barras de Datos) Rojo para identificar el peligro
                ws1.Range($"I8:I{rowTop - 1}").AddConditionalFormat().DataBar(XLColor.FromArgb(255, 99, 71));
            }

            // ── TABLA INFERIOR: CUSTOM TASKS ──
            if (req.CustomTasks != null && req.CustomTasks.Any())
            {
                ws1.Cell("B14").Value = "Detalle de Tareas Adicionales (Fase 4)";
                ws1.Range("B14:C14").Merge().Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromArgb(11, 13, 22)).Font.SetFontColor(XLColor.White);

                ws1.Cell("B15").Value = "Descripción de la Tarea";
                ws1.Cell("C15").Value = "Horas Estimadas";
                ws1.Range("B15:C15").Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.LightGray);

                int rowCustom = 16;
                foreach (var t in req.CustomTasks)
                {
                    ws1.Cell(rowCustom, 2).Value = t.Nombre;
                    ws1.Cell(rowCustom, 3).Value = Math.Round(t.Horas, 1);
                    rowCustom++;
                }
                ws1.Range($"B15:C{rowCustom - 1}").Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin);
            }

            ws1.Columns().AdjustToContents();

            // ─────────────────────────────────────────────────────────────
            // HOJA 2: MATRIZ DE ESFUERZO UNIFICADA (TODAS LAS FASES)
            // ─────────────────────────────────────────────────────────────
            var ws2 = workbook.Worksheets.Add("Matriz de Esfuerzo");
            ws2.Cell("A1").Value = "JUSTIFICACIÓN DETALLADA DE HORAS (TODAS LAS FASES)";
            ws2.Range("A1:E1").Merge().Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromArgb(11, 13, 22)).Font.SetFontColor(XLColor.White);

            var matrizUnificada = new List<dynamic>();

            var matrizReglas = hallazgosConCosto.GroupBy(x => x.Original.FindingType)
                .Select(g => new {
                    Fase = "1. Refactorización",
                    Tarea = (string)g.Key,
                    Ocurrencias = g.Count().ToString(),
                    Modalidad = (string)g.First().ModoCobro,
                    HorasTotales = Math.Round(g.Sum(x => (double)x.CostoAplicado) * req.Multiplicador, 1)
                });
            matrizUnificada.AddRange(matrizReglas);

            if (req.CantidadJobs > 0)
                matrizUnificada.Add(new { Fase = "2. Orquestación", Tarea = "Configuración y migración de Jobs", Ocurrencias = req.CantidadJobs.ToString(), Modalidad = "Por Job", HorasTotales = Math.Round(horasOrqJobs, 1) });

            if (req.IncluirCicd && req.CantidadJobs > 0)
                matrizUnificada.Add(new { Fase = "2. Orquestación", Tarea = "Implementación CI/CD (YAMLs)", Ocurrencias = $"Global + {req.CantidadJobs}", Modalidad = "Fijo + Por Job", HorasTotales = Math.Round(horasOrqCicd, 1) });

            if (req.CantidadTablas > 0)
                matrizUnificada.Add(new { Fase = "3. Calidad (QA)", Tarea = "Cuadratura y Certificación de Tablas", Ocurrencias = req.CantidadTablas.ToString(), Modalidad = "Por Tabla", HorasTotales = Math.Round(horasQa, 1) });

            if (req.CustomTasks != null)
            {
                foreach (var ct in req.CustomTasks)
                    matrizUnificada.Add(new { Fase = "4. Tareas Custom", Tarea = ct.Nombre, Ocurrencias = "1", Modalidad = "Manual", HorasTotales = Math.Round(ct.Horas, 1) });
            }

            ws2.Cell(3, 1).InsertTable(matrizUnificada).Theme = XLTableTheme.TableStyleMedium9;
            ws2.Columns().AdjustToContents();

            // ─────────────────────────────────────────────────────────────
            // HOJA 3: BACKLOG TÉCNICO (EL RECUPERADO)
            // ─────────────────────────────────────────────────────────────
            var ws3 = workbook.Worksheets.Add("Backlog Técnico (Código)");
            var backlogData = hallazgosConCosto.Select(x => new
            {
                Archivo = (string)x.Original.FileName,
                Problema = (string)x.Original.FindingType,
                Severidad = (string)x.Original.Severity,
                Ubicacion = $"Celda {x.Original.CellNumber}, Lín {x.Original.LineNumber}",
                Detalle = (string)x.Original.Details,
                HorasBaseAsignadas = Math.Round((double)x.CostoAplicado, 2)
            }).ToList();

            ws3.Cell(1, 1).InsertTable(backlogData).Theme = XLTableTheme.TableStyleLight10;
            ws3.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }

        // CLASES DTO PARA RECIBIR DATOS DEL MODAL
        public class CotizacionRequest
        {
            public int AnalysisId { get; set; }
            public string DificultadLabel { get; set; }
            public double Multiplicador { get; set; }
            public int CantidadJobs { get; set; }
            public int CantidadTablas { get; set; }
            public bool IncluirCicd { get; set; }
            public List<CotizacionCustomTask> CustomTasks { get; set; } = new List<CotizacionCustomTask>();
        }

        public class CotizacionCustomTask
        {
            public string Nombre { get; set; }
            public double Horas { get; set; }
        }

        public AnalysisSummary CreateSummary(List<Finding> findings, int analysisRunId, DateTime timestamp)
        {
            var summary = new AnalysisSummary
            {
                AnalysisRunId = analysisRunId,
                AnalysisTimestamp = timestamp,
                CriticalCount = findings.Count(f => f.Severity == "Critical"),
                WarningCount = findings.Count(f => f.Severity == "Warning"),
                InfoCount = findings.Count(f => f.Severity == "Info")
            };

            var typesSummary = findings
                .GroupBy(f => f.FindingType)
                .ToDictionary(g => g.Key, g => g.Count());

            summary.FindingTypesSummaryJson = JsonSerializer.Serialize(typesSummary);

            return summary;
        }
    }
}
