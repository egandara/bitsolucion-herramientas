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
    public record WidgetVariableDeclaration(string Name, int CellIndex, int LineIndex, string Content);
    public record ImportInfo(string Name, int CellIndex, int LineIndex, string Content);

    public class NotebookValidatorService
    {
        private readonly ApplicationDbContext _context;
        // Regex para detectar SQL que NO debe borrarse (DML/DDL)
        private static readonly Regex SqlOperationalRegex = new Regex(@"\b(INSERT|UPDATE|DELETE|MERGE|ALTER|CREATE|DROP|TRUNCATE|REPLACE|COPY)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public NotebookValidatorService(ApplicationDbContext context)
        {
            _context = context;
        }

        // --- MOTOR DE GENERACIÓN DE ARCHIVOS CORREGIDOS (HITO 3) ---
        public async Task<byte[]> GenerateCorrectedFilesZipAsync(IEnumerable<(Stream stream, string fileName)> files, List<string> typesToClean)
        {
            using var ms = new MemoryStream();
            using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
            {
                foreach (var file in files)
                {
                    using var reader = new StreamReader(file.stream);
                    var content = await reader.ReadToEndAsync();
                    string correctedContent = content;
                    var baseName = Path.GetFileNameWithoutExtension(file.fileName);

                    if (file.fileName.EndsWith(".ipynb"))
                    {
                        var notebook = JsonSerializer.Deserialize<Notebook>(content, new JsonSerializerOptions { AllowTrailingCommas = true });
                        if (notebook?.Cells != null)
                        {
                            // 1. CORRECCIÓN DE HEADER: Cambia la primera celda por el nombre del archivo
                            if (typesToClean.Contains("Header Incorrecto") && notebook.Cells.Any())
                            {
                                notebook.Cells[0].Source = new List<string> { $"# {baseName}" };
                            }

                            // 2. LIMPIEZA DE CELDAS (%sql y salida del footer)
                            notebook.Cells = notebook.Cells.Where(cell => {
                                if (cell.CellType != "code") return true;
                                var code = string.Join("\n", cell.Source).Trim();

                                // Lógica inteligente para %sql
                                if (typesToClean.Contains("Código SQL en Databricks") && code.StartsWith("%sql", StringComparison.OrdinalIgnoreCase))
                                {
                                    return SqlOperationalRegex.IsMatch(code);
                                }

                                // Si corregimos el footer, eliminamos celdas duplicadas de salida antes de agregar las nuevas
                                if (typesToClean.Contains("Footer Incorrecto") && code.Contains("dbutils.notebook.exit"))
                                {
                                    return false;
                                }

                                return true;
                            }).ToList();

                            // 3. CORRECCIÓN DE FOOTER: Inyecta las dos celdas estándar
                            if (typesToClean.Contains("Footer Incorrecto"))
                            {
                                notebook.Cells.Add(new Cell
                                {
                                    CellType = "code",
                                    Source = new List<string> { "# Mensaje Final" }
                                });
                                notebook.Cells.Add(new Cell
                                {
                                    CellType = "code",
                                    Source = new List<string> { "dbutils.notebook.exit(\"{\\\"coderror\\\":\\\"0\\\", \\\"msgerror\\\":\\\"Notebook termina ejecucion satisfactoriamente\\\"}\")" }
                                });
                            }

                            correctedContent = JsonSerializer.Serialize(notebook, new JsonSerializerOptions { WriteIndented = true });
                        }
                    }

                    var entry = archive.CreateEntry(file.fileName);
                    using var entryStream = entry.Open();
                    using var writer = new StreamWriter(entryStream);
                    await writer.WriteAsync(correctedContent);
                }
            }
            return ms.ToArray();
        }

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
                    var tempFilePath = Path.Combine(tempDirectory, file.fileName);
                    using (var fs = new FileStream(tempFilePath, FileMode.Create)) { await file.stream.CopyToAsync(fs); }

                    if (Path.GetExtension(file.fileName).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        var extractPath = Path.Combine(tempDirectory, Guid.NewGuid().ToString());
                        ZipFile.ExtractToDirectory(tempFilePath, extractPath);
                        var notebookFiles = Directory.GetFiles(extractPath, "*.*", SearchOption.AllDirectories)
                            .Where(f => f.EndsWith(".py") || f.EndsWith(".ipynb"));
                        foreach (var path in notebookFiles) notebooksToProcess.Add((path, Path.GetFileName(path)));
                    }
                    else if (file.fileName.EndsWith(".py") || file.fileName.EndsWith(".ipynb"))
                    {
                        notebooksToProcess.Add((tempFilePath, file.fileName));
                    }
                }

                foreach (var (tempPath, originalName) in notebooksToProcess)
                {
                    var (findings, variables) = await AnalyzeNotebookWithVarsAsync(tempPath, originalName);
                    allFindings.AddRange(findings);
                    fileVariables[originalName] = variables;
                }

                return (allFindings, notebooksToProcess.Count, fileVariables);
            }
            finally
            {
                if (Directory.Exists(tempDirectory)) Directory.Delete(tempDirectory, true);
            }
        }

        private async Task<(List<Finding> findings, List<string> variables)> AnalyzeNotebookWithVarsAsync(string filePath, string originalFileName)
        {
            var findings = new List<Finding>();
            var variables = new List<string>();
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
                    var cells = Regex.Split(fileContent, @"#\s*COMMAND\s*-+")
                        .Select(content => new Cell { CellType = "code", Source = content.Split('\n').ToList() })
                        .ToList();
                    notebook = new Notebook { Cells = cells };
                }

                var varRegex = new Regex(@"^(\w+)\s*=\s*dbutils\.widgets\.get", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                foreach (var cell in notebook.Cells.Where(c => c.CellType == "code"))
                {
                    foreach (var line in cell.Source)
                    {
                        var match = varRegex.Match(line.Trim());
                        if (match.Success) variables.Add(match.Groups[1].Value);
                    }
                }
            }
            catch (Exception e)
            {
                findings.Add(new Finding(originalFileName, "Error", $"No se pudo leer el archivo: {e.Message}", Severity: "Critical"));
                return (findings, variables);
            }

            findings.AddRange(ValidateHeader(notebook.Cells, originalFileName));
            findings.AddRange(ValidateUnusedWidgetVariables(notebook.Cells, originalFileName));
            findings.AddRange(ValidateUnusedImports(notebook.Cells, originalFileName));
            findings.AddRange(ValidateFooter(notebook.Cells, originalFileName));

            var dynamicRules = await _context.ValidationRules.Where(r => r.IsEnabled).ToListAsync();
            var compiledRules = dynamicRules.Select(r => new { Rule = r, Regex = new Regex(r.RegexPattern, RegexOptions.IgnoreCase) }).ToList();

            for (int i = 0; i < notebook.Cells.Count; i++)
            {
                var cell = notebook.Cells[i];
                if (cell.CellType != "code") continue;

                for (int l = 0; l < cell.Source.Count; l++)
                {
                    var line = cell.Source[l];
                    var trimmedLine = line.Trim();

                    if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#") ||
                        trimmedLine.StartsWith("import ", StringComparison.OrdinalIgnoreCase) ||
                        trimmedLine.StartsWith("from ", StringComparison.OrdinalIgnoreCase)) continue;

                    foreach (var cr in compiledRules)
                    {
                        if (cr.Regex.IsMatch(line))
                        {
                            findings.Add(new Finding(originalFileName, cr.Rule.RuleName, cr.Rule.DetailsMessage, i + 1, l + 1, trimmedLine, string.Join("\n", cell.Source), cr.Rule.Severity));
                        }
                    }
                }
            }
            return (findings, variables.Distinct().ToList());
        }

        public byte[] GenerateExcelReportBytes(List<Finding> findings, AnalysisRun runInfo, string logoPath = null)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Reporte de Validación");
            if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath)) ws.AddPicture(logoPath).MoveTo(ws.Cell("A1")).Scale(0.4);
            ws.Cell("C2").Value = "REPORTE DE ANÁLISIS DE NOTEBOOKS";
            ws.Cell("C2").Style.Font.Bold = true;
            ws.Cell("C4").Value = "Usuario:"; ws.Cell("D4").Value = runInfo.User?.Email ?? "Sistema Remoto";
            ws.Cell("C5").Value = "Fecha:"; ws.Cell("D5").Value = runInfo.AnalysisTimestamp.ToString("g");
            var data = findings.Select(f => new { f.FileName, f.FindingType, f.Severity, f.CellNumber, f.LineNumber, f.Content, f.Details }).ToList();
            var table = ws.Cell(8, 1).InsertTable(data);
            table.Theme = XLTableTheme.TableStyleMedium9;
            ws.Columns().AdjustToContents();
            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }

        private IEnumerable<Finding> ValidateHeader(List<Cell> cells, string originalFileName)
        {
            if (cells == null || !cells.Any()) yield break;
            var baseFileName = Path.GetFileNameWithoutExtension(originalFileName);
            var firstCellContent = string.Join("\n", cells.First().Source);
            // Validación estricta: debe coincidir con el formato # NombreArchivo
            if (!firstCellContent.Trim().Equals($"# {baseFileName}", StringComparison.OrdinalIgnoreCase))
                yield return new Finding(originalFileName, "Header Incorrecto", $"El header debe ser exactamente '# {baseFileName}'.", 1, Severity: "Warning");
        }

        private IEnumerable<Finding> ValidateFooter(List<Cell> cells, string fileName)
        {
            if (cells == null || cells.Count < 2) yield break;
            var lastTwoCells = cells.Skip(Math.Max(0, cells.Count - 2)).ToList();

            bool hasMsgFinal = string.Join("", lastTwoCells[0].Source).Contains("# Mensaje Final");
            bool hasExit = string.Join("", lastTwoCells[1].Source).Contains("dbutils.notebook.exit");

            if (!hasMsgFinal || !hasExit)
                yield return new Finding(fileName, "Footer Incorrecto", "Faltan las celdas estándar de cierre (# Mensaje Final y dbutils.notebook.exit).", cells.Count, Severity: "Info");
        }

        private IEnumerable<Finding> ValidateUnusedImports(List<Cell> cells, string fileName) => Enumerable.Empty<Finding>();
        private IEnumerable<Finding> ValidateUnusedWidgetVariables(List<Cell> cells, string fileName) => Enumerable.Empty<Finding>();
    }
}
