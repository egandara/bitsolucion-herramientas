using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NotebookValidator.Web.Models;

namespace NotebookValidator.Web.Services
{
    public class TempTableService
    {
        private static readonly Regex TempTableRegex = new Regex(@"\+\s*platinum_temp_dbX\s*\+\s*['""]+\.([a-zA-Z0-9_]+)", RegexOptions.IgnoreCase);

        // 1. Modificar la firma del método
        public async Task<string> GenerateDeletionNotebookAsync(
            IFormFileCollection files,
            string baseFileName,
            string platinumTempDb,
            string dbLocationPlatinumTemp)
        {
            var tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDirectory);
            var allNotebooksContent = new List<string>();

            try
            {
                // 1. (Lógica de extracción de archivos - Sin cambios)
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

                // 2. (Lógica de análisis - Sin cambios)
                var tempTableNames = new HashSet<string>();
                foreach (var content in allNotebooksContent)
                {
                    // Manejar posible JSON inválido
                    try
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
                    catch (JsonException ex)
                    {
                        // Ignorar este notebook, pero loggear el error sería ideal
                        Console.WriteLine($"Error al deserializar notebook: {ex.Message}");
                    }
                }

                // 3. Generar el nuevo notebook de salida con los nuevos parámetros
                return BuildOutputNotebook(
                    tempTableNames.ToList(),
                    baseFileName,
                    platinumTempDb,
                    dbLocationPlatinumTemp
                );
            }
            finally
            {
                if (Directory.Exists(tempDirectory))
                {
                    Directory.Delete(tempDirectory, recursive: true);
                }
            }
        }

        // 2. Modificar la firma de BuildOutputNotebook
        private string BuildOutputNotebook(
            List<string> tableNames,
            string baseFileName,
            string platinumTempDb,
            string dbLocationPlatinumTemp)
        {
            var notebook = new
            {
                cells = new List<object>(),
                metadata = new { },
                nbformat = 4,
                nbformat_minor = 0
            };

            // 3. [MODIFICADO] Añadir celda de título usando el baseFileName
            notebook.cells.Add(CreateMarkdownCell($"# {baseFileName}"));

            // 4. [MODIFICADO] Añadir celdas de widgets usando los parámetros
            notebook.cells.Add(CreateMarkdownCell("## Captura de Variables"));
            notebook.cells.Add(CreateCodeCell(
                "dbutils.widgets.removeAll()\n\n" +
                // Los nombres (1er param) son fijos, los default (2do param) son los valores de la UI
                $"dbutils.widgets.text(\"platinum_temp_dbW\", \"{platinumTempDb}\", \"01 DB Platinum Temp:\")\n" +
                $"dbutils.widgets.text(\"db_location_platinum_tempW\", \"{dbLocationPlatinumTemp}\", \"02 Location Platinum Temp DB:\")"
            ));

            // 5. (Resto de las celdas - Sin cambios)
            notebook.cells.Add(CreateCodeCell("platinum_temp_dbX = dbutils.widgets.get(\"platinum_temp_dbW\")\nspark.conf.set(\"bci.platinum_temp_dbX\", platinum_temp_dbX)\n\ndb_location_platinum_temp_X = dbutils.widgets.get(\"db_location_platinum_tempW\")\nspark.conf.set(\"bci.db_location_platinum_temp_X\", db_location_platinum_temp_X)"));
            notebook.cells.Add(CreateMarkdownCell("## Funciones"));
            notebook.cells.Add(CreateCodeCell("def sql_safe(query):\n  try:\n    print (\"sqlSafe: query -> \" + query)\n    return spark.sql(query)\n  except Exception as e:\n    dbutils.notebook.exit(\"{\\\"coderror\\\":\\\"20001\\\", \\\"msgerror\\\":\\\"Error grave procesado consulta -> \"+str(e)+\"\\\"}\")"));
            notebook.cells.Add(CreateMarkdownCell("## Eliminacion Tablas Temporales"));

            foreach (var tableName in tableNames.OrderBy(t => t)) // Ordenar para consistencia
            {
                var safeTableNamePart = new string(tableName.Where(char.IsLetterOrDigit).ToArray());
                var variableName = $"paso_tb_del_{safeTableNamePart.Substring(0, Math.Min(safeTableNamePart.Length, 20))}";

                notebook.cells.Add(CreateMarkdownCell($"### Tabla {tableName}"));
                notebook.cells.Add(CreateCodeCell($"{variableName} = \"\"\"DROP TABLE IF EXISTS \"\"\" + platinum_temp_dbX + \".{tableName}\"\"\""));
                notebook.cells.Add(CreateCodeCell($"sql_safe({variableName})"));
                notebook.cells.Add(CreateCodeCell($"dbutils.fs.rm(db_location_platinum_temp_X+\"{tableName}\", True)"));
            }

            notebook.cells.Add(CreateMarkdownCell("## Mensaje Final"));
            notebook.cells.Add(CreateCodeCell("dbutils.notebook.exit(\"{\\\"coderror\\\":\\\"0\\\", \\\"msgerror\\\":\\\"Notebook termina ejecucion satisfactoriamente\\\"}\")"));

            return JsonSerializer.Serialize(notebook, new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
        }

        // (Métodos ayudantes CreateMarkdownCell y CreateCodeCell - Sin cambios)
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