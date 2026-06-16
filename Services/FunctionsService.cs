using NotebookValidator.Web.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using System.Text.Encodings.Web;

namespace NotebookValidator.Web.Services
{
    public class FunctionsService
    {
        private readonly IWebHostEnvironment _env;
        private readonly string _masterPath;
        private readonly string _backupPath;

        public FunctionsService(IWebHostEnvironment env)
        {
            _env = env;
            _masterPath = Path.Combine(_env.WebRootPath, "standards", "Funciones.ipynb");
            _backupPath = Path.Combine(_env.WebRootPath, "standards", "funciones", "backups");

            if (!Directory.Exists(_backupPath))
                Directory.CreateDirectory(_backupPath);
        }

        public async Task<Notebook> GetMasterNotebookAsync()
        {
            if (!File.Exists(_masterPath)) return new Notebook { Cells = new List<Cell>() };
            var content = await File.ReadAllTextAsync(_masterPath);
            var notebook = JsonSerializer.Deserialize<Notebook>(content) ?? new Notebook();

            foreach (var cell in notebook.Cells)
            {
                cell.Outputs ??= new List<object>();
                cell.Metadata ??= new Dictionary<string, object>();

                // ✅ Decodifica escapes unicode literales que puedan estar en el source
                // Ejemplo: \u0022 -> "   |   \u003E -> >   |   \u002B -> +
                cell.Source = cell.Source.Select(line =>
                {
                    try
                    {
                        return line.Contains("\\u")
                            ? Regex.Unescape(line)
                            : line;
                    }
                    catch
                    {
                        return line; // Si falla el unescape, devuelve la línea original
                    }
                }).ToList();
            }

            return notebook;
        }

        public async Task SaveMasterWithBackupAsync(Notebook notebook, string userId)
        {
            // 1. Respaldo automático antes de sobreescribir
            if (File.Exists(_masterPath))
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var fileName = $"Funciones_{timestamp}_{userId}.ipynb";
                File.Copy(_masterPath, Path.Combine(_backupPath, fileName));
            }

            // 2. Objeto con estructura completa válida para Jupyter/Databricks
            var notebookCompleto = new
            {
                nbformat = 4,
                nbformat_minor = 5,
                metadata = new
                {
                    language_info = new { name = "python" }
                },
                cells = notebook.Cells
            };

            // 3. Guardado con codificación relajada y sin omitir campos nulos
            var opciones = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                DefaultIgnoreCondition = JsonIgnoreCondition.Never
            };

            var json = JsonSerializer.Serialize(notebookCompleto, opciones);
            await File.WriteAllTextAsync(_masterPath, json);
        }

        public string GetFunctionName(Cell cell)
        {
            var source = string.Join("", cell.Source);
            var match = Regex.Match(source, @"def\s+(\w+)\s*\(");
            return match.Success ? match.Groups[1].Value : "Celda de Código (Sin definición)";
        }
    }
}
