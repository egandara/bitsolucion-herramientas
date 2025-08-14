using NotebookValidator.Web.Models;
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
                findings.Add(new Finding(originalFileName, "Error de Lectura", $"No se pudo leer o procesar el archivo: {e.Message}"));
                return findings;
            }

            // Validaciones a nivel de NOTEBOOK COMPLETO
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
                    
                    findings.AddRange(ValidateHardcodedPaths(code, originalFileName, cellNumber, code));
                    findings.AddRange(ValidateSqlCommands(code, originalFileName, cellNumber, code));
                }
            }

            return findings;
        }
        
        // --- MÉTODOS DE VALIDACIÓN ---

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
                    CellSourceCode: firstCellContent
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
                yield return new Finding(fileName, "Footer Faltante", "No se encontró la celda Markdown con 'Mensaje Final' al final del notebook.");
                yield break;
            }

            int exitCellIndex = finalMessageIndex + 1;

            if (exitCellIndex >= cells.Count)
            {
                yield return new Finding(fileName, "Footer Incorrecto", "Falta la celda de código con 'dbutils.notebook.exit' después del Mensaje Final.", exitCellIndex);
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
                    CellSourceCode: exitCellContent
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
                    CellSourceCode: extraCellContent
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
                        string.Join("\n", allCodeByCell[import.CellIndex])
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
                        string.Join("\n", cellCodeMap[decl.CellIndex])
                    );
                }
            }
        }

        private IEnumerable<Finding> ValidateSqlCommands(string code, string fileName, int cellNumber, string cellSourceCode)
        {
            var sqlMagicRegex = new Regex(@"^\s*%%?sql", RegexOptions.IgnoreCase);
            var lines = code.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                if (sqlMagicRegex.IsMatch(lines[i]))
                {
                    yield return new Finding(
                        fileName,
                        "Uso de %sql",
                        "El comando mágico SQL debe ser reemplazado por spark.sql() o quitado.",
                        cellNumber,
                        i + 1,
                        lines[i].Trim(),
                        cellSourceCode
                    );
                }
            }
        }

        private IEnumerable<Finding> ValidateHardcodedPaths(string code, string fileName, int cellNumber, string cellSourceCode)
        {
            var hardcodedPathRegex = new Regex(@"(""|').*(\/|\\).*(""|')");
            var lines = code.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var trimmedLine = line.Trim();
                if (trimmedLine.StartsWith("#") ||
                    trimmedLine.ToUpper().Contains("COMMENT") ||
                    trimmedLine.Contains("dbutils.notebook.exit") ||
                    trimmedLine.StartsWith("print(", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                foreach (Match match in hardcodedPathRegex.Matches(line))
                {
                    var foundPath = match.Value;
                    int slashCount = foundPath.Count(c => c == '/' || c == '\\');
                    if (slashCount <= 1)
                    {
                        continue;
                    }
                    var cleanPath = foundPath.Trim('"', '\'');
                    if (cleanPath.StartsWith("C/C", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    if (foundPath.Contains('+') || cleanPath.StartsWith("http"))
                    {
                        continue;
                    }
                    yield return new Finding(
                        fileName,
                        "Ruta en duro",
                        "Posible ruta en duro encontrada.",
                        cellNumber,
                        i + 1,
                        line.Trim(),
                        cellSourceCode
                    );
                }
            }
        }
    }
}