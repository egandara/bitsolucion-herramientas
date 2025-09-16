using NotebookValidator.Web.Data;
using NotebookValidator.Web.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NotebookValidator.Web.Services
{
    // Records auxiliares para almacenar información durante el análisis
    public record WidgetVariableDeclaration(string Name, int CellIndex, int LineIndex, string Content);
    public record ImportInfo(string Name, int CellIndex, int LineIndex, string Content);

    public class NotebookValidatorService
    {
        // --- INYECCIÓN DE DEPENDENCIA ---
        private readonly ApplicationDbContext _context;
        private List<ValidationRule> _dynamicRules; // Caché de reglas

        // --- CONSTRUCTOR MODIFICADO ---
        public NotebookValidatorService(ApplicationDbContext context)
        {
            _context = context;
            _dynamicRules = new List<ValidationRule>();
        }

        public async Task<List<Finding>> AnalyzeNotebookAsync(string filePath, string originalFileName)
        {
            var findings = new List<Finding>();
            Notebook notebook;

            try
            {
                var fileContent = await File.ReadAllTextAsync(filePath);
                if (filePath.EndsWith(".ipynb", StringComparison.OrdinalIgnoreCase))
                {
                    notebook = JsonSerializer.Deserialize<Notebook>(fileContent, new JsonSerializerOptions { AllowTrailingCommas = true }) ?? new Notebook();
                }
                else
                {
                    notebook = new Notebook
                    {
                        Cells = new List<Cell> { new Cell { CellType = "code", Source = fileContent.Split('\n').ToList() } }
                    };
                }
            }
            catch (Exception e)
            {
                findings.Add(new Finding(originalFileName, "Error de Lectura", $"No se pudo leer o procesar el archivo: {e.Message}", Severity: "Critical"));
                return findings;
            }

            // --- CARGA DINÁMICA DE REGLAS ---
            _dynamicRules = await _context.ValidationRules.Where(r => r.IsEnabled).ToListAsync();

            // Validaciones ESTRUCTURALES (Hard-coded, se mantienen)
            findings.AddRange(ValidateHeader(notebook.Cells, originalFileName));
            findings.AddRange(ValidateUnusedWidgetVariables(notebook.Cells, originalFileName));
            findings.AddRange(ValidateUnusedImports(notebook.Cells, originalFileName));
            findings.AddRange(ValidateFooter(notebook.Cells, originalFileName));

            // Bucle para validaciones a nivel de CELDA
            for (int i = 0; i < notebook.Cells.Count; i++)
            {
                var cell = notebook.Cells[i];
                var cellNumber = i + 1;
                if (cell.CellType == "code")
                {
                    var code = string.Join("\n", cell.Source);

                    // --- APLICA LAS REGLAS DINÁMICAS (REEMPLAZA LOS MÉTODOS ANTIGUOS) ---
                    findings.AddRange(ApplyDynamicCellRules(code, originalFileName, cellNumber, code));
                }
            }

            return findings;
        }

        // --- MÉTODOS DE VALIDACIÓN ESTRUCTURAL (AÑADIMOS SEVERIDAD) ---

        private IEnumerable<Finding> ValidateHeader(List<Cell> cells, string originalFileName)
        {
            if (cells == null || !cells.Any())
            {
                yield break;
            }

            var baseFileName = Path.GetFileNameWithoutExtension(originalFileName);
            var firstCell = cells.First();
            var firstCellContent = string.Join("\n", firstCell.Source);

            if (!firstCellContent.Contains(baseFileName, StringComparison.OrdinalIgnoreCase))
            {
                yield return new Finding(
                    originalFileName,
                    "Header Incorrecto",
                    $"La primera celda no contiene el nombre del archivo '{baseFileName}'.",
                    CellNumber: 1,
                    LineNumber: null,
                    Content: firstCellContent.Split('\n').FirstOrDefault()?.Trim(),
                    CellSourceCode: firstCellContent,
                    Severity: "Warning" // <-- SEVERIDAD AÑADIDA
                );
            }
        }

        private IEnumerable<Finding> ValidateFooter(List<Cell> cells, string fileName)
        {
            if (cells == null || !cells.Any()) yield break;

            int finalMessageIndex = -1;
            for (int i = cells.Count - 1; i >= 0; i--)
            {
                if (cells[i].CellType == "markdown" && string.Join("", cells[i].Source).Contains("Mensaje Final"))
                {
                    finalMessageIndex = i;
                    break;
                }
            }

            if (finalMessageIndex == -1)
            {
                yield return new Finding(fileName, "Footer Faltante", "No se encontró la celda Markdown con 'Mensaje Final' al final del notebook.", Severity: "Info");
                yield break;
            }

            int exitCellIndex = finalMessageIndex + 1;

            if (exitCellIndex >= cells.Count)
            {
                yield return new Finding(fileName, "Footer Incorrecto", "Falta la celda de código con 'dbutils.notebook.exit' después del Mensaje Final.", exitCellIndex, Severity: "Info");
                yield break;
            }

            var exitCell = cells[exitCellIndex];
            var exitCellContent = string.Join("\n", exitCell.Source);
            if (exitCell.CellType != "code" || !exitCellContent.Contains("dbutils.notebook.exit"))
            {
                yield return new Finding(
                    fileName,
                    "Footer Incorrecto",
                    "La celda siguiente al 'Mensaje Final' no es de código o no contiene 'dbutils.notebook.exit'.",
                    exitCellIndex + 1,
                    Content: exitCellContent.Split('\n').FirstOrDefault()?.Trim(),
                    CellSourceCode: exitCellContent,
                    Severity: "Info"
                );
            }

            if (cells.Count > exitCellIndex + 1)
            {
                var extraCell = cells[exitCellIndex + 1];
                var extraCellContent = string.Join("\n", extraCell.Source);
                yield return new Finding(
                   fileName,
                   "Código posterior al final",
                   "Se encontró una celda después de 'dbutils.notebook.exit', lo cual no está permitido.",
                   exitCellIndex + 2,
                   Content: extraCellContent.Split('\n').FirstOrDefault()?.Trim(),
                   CellSourceCode: extraCellContent,
                   Severity: "Info"
               );
            }
        }

        private IEnumerable<Finding> ValidateUnusedImports(List<Cell> cells, string fileName)
        {
            var imports = new List<ImportInfo>();
            var allCodeByCell = new Dictionary<int, string[]>();
            var importRegex = new Regex(@"^\s*import\s+([a-zA-Z0-9_]+)(?:\s+as\s+([a-zA-Z0-9_]+))?");
            var fromImportRegex = new Regex(@"^\s*from\s+[a-zA-Z0-9_.]+\s+import\s+([a-zA-Z0-9_]+)(?:\s+as\s+([a-zA-Z0-9_]+))?");

            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i].CellType != "code") continue;
                var lines = cells[i].Source.ToArray();
                allCodeByCell[i] = lines;
                for (int j = 0; j < lines.Length; j++)
                {
                    var line = lines[j].Trim();
                    var importMatch = importRegex.Match(line);
                    if (importMatch.Success)
                    {
                        var name = !string.IsNullOrEmpty(importMatch.Groups[2].Value) ? importMatch.Groups[2].Value : importMatch.Groups[1].Value;
                        imports.Add(new ImportInfo(name, i, j, line));
                        continue;
                    }
                    var fromImportMatch = fromImportRegex.Match(line);
                    if (fromImportMatch.Success)
                    {
                        var name = !string.IsNullOrEmpty(fromImportMatch.Groups[2].Value) ? fromImportMatch.Groups[2].Value : fromImportMatch.Groups[1].Value;
                        imports.Add(new ImportInfo(name, i, j, line));
                    }
                }
            }

            foreach (var import in imports)
            {
                bool isUsed = false;
                var usageRegex = new Regex(@"\b" + Regex.Escape(import.Name) + @"\b");
                foreach (var cellEntry in allCodeByCell)
                {
                    for (int lineIdx = 0; lineIdx < cellEntry.Value.Length; lineIdx++)
                    {
                        if (cellEntry.Key == import.CellIndex && lineIdx == import.LineIndex) continue;
                        if (usageRegex.IsMatch(cellEntry.Value[lineIdx]))
                        {
                            isUsed = true;
                            break;
                        }
                    }
                    if (isUsed) break;
                }

                if (!isUsed)
                {
                    yield return new Finding(
                        fileName,
                        "Importación no Usada",
                        $"La librería o módulo '{import.Name}' se importa pero no se utiliza en el notebook.",
                        import.CellIndex + 1,
                        import.LineIndex + 1,
                        import.Content,
                        string.Join("\n", allCodeByCell[import.CellIndex]),
                        Severity: "Warning" // <-- SEVERIDAD AÑADIDA
                    );
                }
            }
        }

        private IEnumerable<Finding> ValidateUnusedWidgetVariables(List<Cell> cells, string fileName)
        {
            var declarations = new List<WidgetVariableDeclaration>();
            var cellCodeMap = new Dictionary<int, string[]>();
            var assignmentRegex = new Regex(@"^\s*([a-zA-Z0-9_]+)\s*=\s*dbutils\.widgets\.get\s*\(");

            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i].CellType == "code")
                {
                    var lines = cells[i].Source.ToArray();
                    cellCodeMap[i] = lines;
                    for (int j = 0; j < lines.Length; j++)
                    {
                        var match = assignmentRegex.Match(lines[j]);
                        if (match.Success)
                        {
                            declarations.Add(new WidgetVariableDeclaration(match.Groups[1].Value, i, j, lines[j].Trim()));
                        }
                    }
                }
            }

            foreach (var decl in declarations)
            {
                bool isUsed = false;
                var usageRegex = new Regex(@"\b" + Regex.Escape(decl.Name) + @"\b");
                foreach (var entry in cellCodeMap)
                {
                    for (int lineIdx = 0; lineIdx < entry.Value.Length; lineIdx++)
                    {
                        if (entry.Key == decl.CellIndex && lineIdx == decl.LineIndex) continue;
                        var line = entry.Value[lineIdx].Trim();
                        if (line.StartsWith("print(") || line.StartsWith("spark.conf.set(")) continue;
                        if (usageRegex.IsMatch(line))
                        {
                            isUsed = true;
                            break;
                        }
                    }
                    if (isUsed) break;
                }

                if (!isUsed)
                {
                    yield return new Finding(
                        fileName,
                        "Variable de Widget no usada",
                        $"La variable '{decl.Name}' se obtiene de un widget pero no se usa posteriormente (ignorando prints y spark.conf.set).",
                        decl.CellIndex + 1,
                        decl.LineIndex + 1,
                        decl.Content,
                        string.Join("\n", cellCodeMap[decl.CellIndex]),
                        Severity: "Warning" // <-- SEVERIDAD AÑADIDA
                    );
                }
            }
        }

        // --- MÉTODO DINÁMICO (CORRECCIÓN CS1631) ---
        private IEnumerable<Finding> ApplyDynamicCellRules(string code, string fileName, int cellNumber, string cellSourceCode)
        {
            // --- PASO 1: Lista para errores de reglas ---
            var errorFindings = new List<Finding>();
            var compiledRules = new List<(ValidationRule rule, Regex regex)>();

            foreach (var rule in _dynamicRules)
            {
                try
                {
                    // Intenta compilar el Regex
                    var regex = new Regex(rule.RegexPattern, RegexOptions.IgnoreCase);
                    compiledRules.Add((rule, regex));
                }
                catch (Exception ex)
                {
                    // --- CORRECCIÓN ---
                    // No usamos 'yield return' aquí. Lo añadimos a una lista.
                    errorFindings.Add(new Finding(
                        fileName,
                        "Error de Regla",
                        $"La regla '{rule.RuleName}' (ID: {rule.Id}) tiene un patrón Regex inválido: {ex.Message}",
                        cellNumber,
                        Severity: "Critical"
                    ));
                }
            }

            // --- PASO 2: Aplicar los Regex compilados a cada línea ---
            var lines = code.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var trimmedLine = line.Trim();

                // Optimización: Ignorar líneas de comentario o importación
                if (trimmedLine.StartsWith("#") ||
                    trimmedLine.StartsWith("import", StringComparison.OrdinalIgnoreCase) ||
                    trimmedLine.StartsWith("from", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Itera por las reglas que SÍ compilaron
                foreach (var (rule, regex) in compiledRules)
                {
                    // Esta sección ya NO está en un try...catch
                    if (regex.IsMatch(line))
                    {
                        // Este 'yield return' es legal
                        yield return new Finding(
                            fileName,
                            rule.RuleName,
                            rule.DetailsMessage,
                            cellNumber,
                            i + 1,
                            line.Trim(),
                            cellSourceCode,
                            rule.Severity
                        );
                    }
                }
            }

            // --- PASO 3: Devolver todos los errores de reglas al final ---
            foreach (var error in errorFindings)
            {
                yield return error;
            }
        }

        // --- MÉTODOS DE VALIDACIÓN OBSOLETOS (ELIMINADOS) ---
        // private IEnumerable<Finding> ValidateSqlCommands(...)
        // private IEnumerable<Finding> ValidateHardcodedPaths(...)
        // private IEnumerable<Finding> ValidateHardcodedDatabases(...)
    }
}