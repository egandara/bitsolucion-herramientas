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
        // 1. [NUEVO] Definimos una lista flexible de variaciones para el nombre de la variable de base de datos.
        // Esto atrapará: db_platinum_tmpX, platinum_temp_dbX, db_plat_temp_X, etc.
        private const string DbVars = @"(?:platinum_temp_db|db_plat_temp|db_platinum_tmp|db_platinum_temp|platinum_tmp_db|db_plat_tmp)_?X?";

        // 2. [ACTUALIZADO] Usamos interpolación ($"") para inyectar las variaciones en el Regex.
        // Sólo detecta tablas creadas dentro de la misma celda en una sentencia CREATE TABLE
        private static readonly Regex CreateTableWithDbVarRegex = new Regex(
            $@"CREATE\s+TABLE(?:\s+IF\s+NOT\s+EXISTS)?[\s\S]{{0,800}}?(?:\+\s*{DbVars}\s*\+\s*['""]{{1,3}}\s*\.\s*|(?:\+\s*{DbVars}\s*\+\s*['""]\s*\+\s*['""]\s*\.\s*)|(?:\b{DbVars}\s*\.\s*))([A-Za-z0-9_]+)",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

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

                var tempTableNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var content in allNotebooksContent)
                {
                    try
                    {
                        var notebook = JsonSerializer.Deserialize<Notebook>(content, new JsonSerializerOptions { AllowTrailingCommas = true });
                        if (notebook?.Cells == null) continue;

                        foreach (var cell in notebook.Cells)
                        {
                            if (cell.CellType == "code")
                            {
                                // Unimos con espacio para mantener separadores sin perder palabras por saltos de línea
                                var code = string.Join(" ", cell.Source);

                                // Extraemos sólo tablas que aparecen en una sentencia CREATE TABLE en la misma celda
                                var matches = CreateTableWithDbVarRegex.Matches(code);
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
                        Console.WriteLine($"Error al deserializar notebook: {ex.Message}");
                    }
                }

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

            notebook.cells.Add(CreateMarkdownCell($"# {baseFileName}"));

            notebook.cells.Add(CreateMarkdownCell("## Captura de Variables"));
            notebook.cells.Add(CreateCodeCell(
                "dbutils.widgets.removeAll()\n\n" +
                $"dbutils.widgets.text(\"platinum_temp_dbW\", \"{platinumTempDb}\", \"01 DB Platinum Temp:\")\n" +
                $"dbutils.widgets.text(\"db_location_platinum_tempW\", \"{dbLocationPlatinumTemp}\", \"02 Location Platinum Temp DB:\")"
            ));

            notebook.cells.Add(CreateCodeCell("platinum_temp_dbX = dbutils.widgets.get(\"platinum_temp_dbW\")\nspark.conf.set(\"bci.platinum_temp_dbX\", platinum_temp_dbX)\n\ndb_location_platinum_temp_X = dbutils.widgets.get(\"db_location_platinum_tempW\")\nspark.conf.set(\"bci.db_location_platinum_temp_X\", db_location_platinum_temp_X)"));
            notebook.cells.Add(CreateMarkdownCell("## Funciones"));
            notebook.cells.Add(CreateCodeCell("def sql_safe(query):\n  try:\n    print (\"sqlSafe: query -> \" + query)\n    return spark.sql(query)\n  except Exception as e:\n    dbutils.notebook.exit(\"{\\\"coderror\\\":\\\"20001\\\", \\\"msgerror\\\":\\\"Error grave procesado consulta -> \"+str(e)+\"\\\"}\")"));
            notebook.cells.Add(CreateMarkdownCell("## Eliminacion Tablas Temporales"));

            foreach (var tableName in tableNames.OrderBy(t => t))
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
