using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NotebookValidator.Web.Models; // Asegúrate de que el namespace de tus modelos sea correcto

namespace NotebookValidator.Web.Services
{
    public class TempTableService
    {
        // Expresión regular para encontrar tablas creadas con la base temporal.
        // Usa un verbatim string (@"...") para manejar correctamente las barras invertidas.
        private static readonly Regex TempTableRegex = new Regex(@"\+\s*platinum_temp_dbX\s*\+\s*['""]+\.([a-zA-Z0-9_]+)", RegexOptions.IgnoreCase);

        public async Task<string> GenerateDeletionNotebookAsync(IFormFileCollection files)
        {
            var tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDirectory);
            var allNotebooksContent = new List<string>();

            try
            {
                // 1. Extraer el contenido de todos los notebooks (incluyendo los que están en ZIPs)
                foreach (var file in files)
                {
                    var tempFilePath = Path.Combine(tempDirectory, file.FileName);
                    using (var stream = new FileStream(tempFilePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    if (Path.GetExtension(file.FileName).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        var extractPath = Path.Combine(tempDirectory, Guid.NewGuid().ToString());
                        Directory.CreateDirectory(extractPath);
                        ZipFile.ExtractToDirectory(tempFilePath, extractPath);
                        var notebookFilesInZip = Directory.GetFiles(extractPath, "*.ipynb", SearchOption.AllDirectories);
                        foreach (var notebookPath in notebookFilesInZip)
                        {
                            allNotebooksContent.Add(await File.ReadAllTextAsync(notebookPath));
                        }
                    }
                    else if (file.FileName.EndsWith(".ipynb"))
                    {
                        allNotebooksContent.Add(await File.ReadAllTextAsync(tempFilePath));
                    }
                }

                // 2. Analizar el contenido y extraer los nombres de las tablas
                var tempTableNames = new HashSet<string>();
                foreach (var content in allNotebooksContent)
                {
                    var notebook = JsonSerializer.Deserialize<Notebook>(content, new JsonSerializerOptions { AllowTrailingCommas = true });
                    if (notebook?.Cells == null) continue;

                    foreach (var cell in notebook.Cells)
                    {
                        if (cell.CellType == "code")
                        {
                            var code = string.Join("", cell.Source);
                            var matches = TempTableRegex.Matches(code);
                            foreach (Match match in matches)
                            {
                                if (match.Groups.Count > 1)
                                {
                                    tempTableNames.Add(match.Groups[1].Value);
                                }
                            }
                        }
                    }
                }

                // 3. Generar el nuevo notebook de salida
                return BuildOutputNotebook(tempTableNames.ToList());
            }
            finally
            {
                if (Directory.Exists(tempDirectory))
                {
                    Directory.Delete(tempDirectory, recursive: true);
                }
            }
        }

        private string BuildOutputNotebook(List<string> tableNames)
        {
            var notebook = new
            {
                cells = new List<object>(),
                metadata = new { },
                nbformat = 4,
                nbformat_minor = 0
            };

            // Añadir celdas estáticas iniciales
            notebook.cells.Add(CreateMarkdownCell("# BCI_05_eliminacion_tablas_temporales_generado"));
            notebook.cells.Add(CreateMarkdownCell("## Captura de Variables"));
            notebook.cells.Add(CreateCodeCell("dbutils.widgets.removeAll()\n\ndbutils.widgets.text(\"platinum_temp_dbW\",\"\",\"01 platinum temp db:\")\ndbutils.widgets.text(\"db_location_platinum_temp_W\",\"\",\"02 platinum temp db:\")"));
            notebook.cells.Add(CreateCodeCell("platinum_temp_dbX = dbutils.widgets.get(\"platinum_temp_dbW\")\nspark.conf.set(\"bci.platinum_temp_dbX\", platinum_temp_dbX)\n\ndb_location_platinum_temp_X = dbutils.widgets.get(\"db_location_platinum_temp_W\")\nspark.conf.set(\"bci.db_location_platinum_temp_X\", db_location_platinum_temp_X)"));
            notebook.cells.Add(CreateMarkdownCell("## Funciones"));
            notebook.cells.Add(CreateCodeCell("def sql_safe(query):\n  try:\n    print (\"sqlSafe: query -> \" + query)\n    return spark.sql(query)\n  except Exception as e:\n    dbutils.notebook.exit(\"{\\\"coderror\\\":\\\"20001\\\", \\\"msgerror\\\":\\\"Error grave procesado consulta -> \"+str(e)+\"\\\"}\")"));
            notebook.cells.Add(CreateMarkdownCell("## Eliminacion Tablas Temporales"));

            // Añadir celdas dinámicas para cada tabla encontrada
            foreach (var tableName in tableNames)
            {
                var safeTableNamePart = new string(tableName.Where(char.IsLetterOrDigit).ToArray());
                var variableName = $"paso_tb_del_{safeTableNamePart.Substring(0, Math.Min(safeTableNamePart.Length, 20))}";

                notebook.cells.Add(CreateMarkdownCell($"### Tabla {tableName}"));
                notebook.cells.Add(CreateCodeCell($"{variableName} = \"\"\"DROP TABLE IF EXISTS \"\"\" + platinum_temp_dbX + \".{tableName}\"\"\""));
                notebook.cells.Add(CreateCodeCell($"sql_safe({variableName})"));
                notebook.cells.Add(CreateCodeCell($"dbutils.fs.rm(db_location_platinum_temp_X+\"/{tableName}\", True)"));
            }

            // Añadir celdas finales
            notebook.cells.Add(CreateMarkdownCell("## Mensaje Final"));
            notebook.cells.Add(CreateCodeCell("dbutils.notebook.exit(\"{\\\"coderror\\\":\\\"0\\\", \\\"msgerror\\\":\\\"Notebook termina ejecucion satisfactoriamente\\\"}\")"));
            
            return JsonSerializer.Serialize(notebook, new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
        }
        
        // Métodos ayudantes para crear celdas de notebook con el formato correcto
        private object CreateMarkdownCell(string source)
        {
            return new { cell_type = "markdown", metadata = new { }, source = source.Split('\n') };
        }

        private object CreateCodeCell(string source)
        {
            return new { cell_type = "code", execution_count = (object)null, metadata = new { }, outputs = new List<object>(), source = source.Split('\n') };
        }
    }
}