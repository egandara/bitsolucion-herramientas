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
    // Un record simple para devolver los resultados
    public record ParameterFinding(string NotebookName, string ParameterName, int LineNumber, string Content);

    public class ParameterValidationService
    {
        private readonly ApplicationDbContext _context;
        // Regex para encontrar widgets: dbutils.widgets.text("nombre_parametro"...)
        private static readonly Regex WidgetPattern = new Regex(@"dbutils\.widgets\.(?:text|dropdown|combobox|multiselect)\s*\(\s*[""']([^""']+)[""']");

        public ParameterValidationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<ParameterFinding>> ValidateParametersAsync(IFormFileCollection files)
        {
            // 1. Obtener la lista de parámetros permitidos desde la BD
            var allowedParameters = await _context.AllowedParameters.Select(p => p.Name).ToHashSetAsync();
            var invalidParameters = new List<ParameterFinding>();

            var tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDirectory);

            try
            {
                // 2. Extraer el contenido de todos los notebooks (incluyendo ZIPs)
                foreach (var file in files)
                {
                    var tempFilePath = Path.Combine(tempDirectory, file.FileName);
                    using (var stream = new FileStream(tempFilePath, FileMode.Create)) { await file.CopyToAsync(stream); }

                    if (Path.GetExtension(file.FileName).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        var extractPath = Path.Combine(tempDirectory, Guid.NewGuid().ToString());
                        Directory.CreateDirectory(extractPath);
                        ZipFile.ExtractToDirectory(tempFilePath, extractPath);
                        var notebookFilesInZip = Directory.GetFiles(extractPath, "*.*", SearchOption.AllDirectories)
                            .Where(f => f.EndsWith(".py") || f.EndsWith(".ipynb"));

                        foreach (var notebookPath in notebookFilesInZip)
                        {
                            await ProcessNotebook(notebookPath, Path.GetFileName(notebookPath), allowedParameters, invalidParameters);
                        }
                    }
                    else if (file.FileName.EndsWith(".py") || file.FileName.EndsWith(".ipynb"))
                    {
                        await ProcessNotebook(tempFilePath, file.FileName, allowedParameters, invalidParameters);
                    }
                }
                return invalidParameters;
            }
            finally
            {
                if (Directory.Exists(tempDirectory)) { Directory.Delete(tempDirectory, recursive: true); }
            }
        }

        private async Task ProcessNotebook(string filePath, string originalName, HashSet<string> allowedParameters, List<ParameterFinding> invalidParameters)
        {
            var content = await File.ReadAllTextAsync(filePath);
            var notebook = JsonSerializer.Deserialize<Notebook>(content, new JsonSerializerOptions { AllowTrailingCommas = true });

            if (notebook?.Cells == null) return;

            foreach (var cell in notebook.Cells)
            {
                if (cell.CellType != "code") continue;

                var lines = cell.Source.ToArray();
                for (int i = 0; i < lines.Length; i++)
                {
                    var matches = WidgetPattern.Matches(lines[i]);
                    foreach (Match match in matches)
                    {
                        if (match.Groups.Count > 1)
                        {
                            var paramName = match.Groups[1].Value;
                            if (!allowedParameters.Contains(paramName))
                            {
                                invalidParameters.Add(new ParameterFinding(originalName, paramName, i + 1, lines[i].Trim()));
                            }
                        }
                    }
                }
            }
        }
    }
}