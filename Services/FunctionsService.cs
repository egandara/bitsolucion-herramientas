using NotebookValidator.Web.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

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
            return JsonSerializer.Deserialize<Notebook>(content) ?? new Notebook();
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

            // 2. Guardado
            var json = JsonSerializer.Serialize(notebook, new JsonSerializerOptions { WriteIndented = true });
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
