using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NotebookValidator.Web.Data;
using NotebookValidator.Web.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NotebookValidator.Web.Services
{
    public record ParameterFinding(string NotebookName, string ParameterName, int LineNumber, string Content);

    // Estructura para resumir la salud de cada archivo procesado
    public class FileValidationSummary
    {
        public string FileName { get; set; }
        public string Status { get; set; } // "Correcto" (Verde), "Pendiente" (Rojo), "Corregido" (Azul)
        public int TotalFindings { get; set; }
    }

    public class ParameterValidationService
    {
        private readonly ApplicationDbContext _context;
        private static readonly Regex WidgetPattern = new Regex(@"dbutils\.widgets\.(?:text|dropdown|combobox|multiselect)\s*\(\s*[""']([^""']+)[""']");

        public ParameterValidationService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Método principal adaptado para persistir archivos en la caché de análisis activa
        public async Task<(List<ParameterFinding> Findings, List<FileValidationSummary> Summaries)> ValidateAndCacheParametersAsync(IFormFileCollection files, string targetDirectory)
        {
            var allowedParameters = await _context.AllowedParameters.Select(p => p.Name).ToHashSetAsync();
            var invalidParameters = new List<ParameterFinding>();
            var summaries = new List<FileValidationSummary>();

            if (Directory.Exists(targetDirectory)) { Directory.Delete(targetDirectory, recursive: true); }
            Directory.CreateDirectory(targetDirectory);

            foreach (var file in files)
            {
                var tempFilePath = Path.Combine(targetDirectory, file.FileName);
                using (var stream = new FileStream(tempFilePath, FileMode.Create)) { await file.CopyToAsync(stream); }

                if (Path.GetExtension(file.FileName).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    var extractPath = Path.Combine(targetDirectory, "Extracted_" + Guid.NewGuid().ToString().Substring(0, 8));
                    Directory.CreateDirectory(extractPath);
                    ZipFile.ExtractToDirectory(tempFilePath, extractPath);

                    var notebookFilesInZip = Directory.GetFiles(extractPath, "*.*", SearchOption.AllDirectories)
                        .Where(f => f.EndsWith(".py") || f.EndsWith(".ipynb"));

                    foreach (var notebookPath in notebookFilesInZip)
                    {
                        var filename = Path.GetFileName(notebookPath);
                        var currentFindings = new List<ParameterFinding>();
                        await ProcessNotebook(notebookPath, filename, allowedParameters, currentFindings);

                        invalidParameters.AddRange(currentFindings);
                        summaries.Add(new FileValidationSummary
                        {
                            FileName = filename,
                            TotalFindings = currentFindings.Count,
                            Status = currentFindings.Count == 0 ? "Correcto" : "Pendiente"
                        });

                        // Mover al directorio raíz de caché para simplificar descargas posteriores
                        var destPath = Path.Combine(targetDirectory, filename);
                        if (!File.Exists(destPath)) File.Move(notebookPath, destPath);
                    }
                    // Limpieza del archivo ZIP original y carpeta temporal interna de extracción
                    File.Delete(tempFilePath);
                    Directory.Delete(extractPath, true);
                }
                else if (file.FileName.EndsWith(".py") || file.FileName.EndsWith(".ipynb"))
                {
                    var currentFindings = new List<ParameterFinding>();
                    await ProcessNotebook(tempFilePath, file.FileName, allowedParameters, currentFindings);

                    invalidParameters.AddRange(currentFindings);
                    summaries.Add(new FileValidationSummary
                    {
                        FileName = file.FileName,
                        TotalFindings = currentFindings.Count,
                        Status = currentFindings.Count == 0 ? "Correcto" : "Pendiente"
                    });
                }
            }
            return (invalidParameters, summaries);
        }

        private async Task ProcessNotebook(string filePath, string originalName, HashSet<string> allowedParameters, List<ParameterFinding> invalidParameters)
        {
            var content = await File.ReadAllTextAsync(filePath);
            var extension = Path.GetExtension(filePath).ToLower();

            if (extension == ".ipynb")
            {
                try
                {
                    var notebook = JsonSerializer.Deserialize<Notebook>(content, new JsonSerializerOptions { AllowTrailingCommas = true });
                    if (notebook?.Cells == null) return;

                    foreach (var cell in notebook.Cells)
                    {
                        if (cell.CellType != "code") continue;
                        var lines = cell.Source.ToArray();
                        for (int i = 0; i < lines.Length; i++)
                        {
                            ProcessLineContent(lines[i], i + 1, originalName, allowedParameters, invalidParameters);
                        }
                    }
                }
                catch (JsonException)
                {
                    ProcessPlainTextContent(content, originalName, allowedParameters, invalidParameters);
                }
            }
            else
            {
                ProcessPlainTextContent(content, originalName, allowedParameters, invalidParameters);
            }
        }

        private void ProcessPlainTextContent(string content, string originalName, HashSet<string> allowedParameters, List<ParameterFinding> invalidParameters)
        {
            var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
            {
                ProcessLineContent(lines[i], i + 1, originalName, allowedParameters, invalidParameters);
            }
        }

        private void ProcessLineContent(string lineContent, int lineNumber, string originalName, HashSet<string> allowedParameters, List<ParameterFinding> invalidParameters)
        {
            var matches = WidgetPattern.Matches(lineContent);
            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    var paramName = match.Groups[1].Value;
                    if (!allowedParameters.Contains(paramName))
                    {
                        invalidParameters.Add(new ParameterFinding(originalName, paramName, lineNumber, lineContent.Trim()));
                    }
                }
            }
        }
    }
}
