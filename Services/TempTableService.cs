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
        private const string DbVars = @"(?:platinum_temp_db|db_plat_temp|db_platinum_tmp|db_platinum_temp|platinum_tmp_db|db_plat_tmp)_?X?";

        private static readonly Regex CreateTableWithDbVarRegex = new Regex(
            $@"CREATE\s+TABLE(?:\s+IF\s+NOT\s+EXISTS)?[\s\S]{{0,800}}?(?:\+\s*{DbVars}\s*\+\s*['""]{{1,3}}\s*\.\s*|(?:\+\s*{DbVars}\s*\+\s*['""]\s*\+\s*['""]\s*\.\s*)|(?:\b{DbVars}\s*\.\s*))([A-Za-z0-9_]+)",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        public async Task<(string notebookJson, List<string> tablesFound)> GenerateDeletionNotebookAsync(
            IFormFileCollection files,
            string baseFileName,
            string platinumTempDb,
            string dbLocationPlatinumTemp)
        {
            var tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDirectory);

            var filesToProcess = new List<(string Content, string Extension)>();

            try
            {
                foreach (var file in files)
                {
                    var tempFilePath = Path.Combine(tempDirectory, file.FileName);
                    using (var stream = new FileStream(tempFilePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    string ext = Path.GetExtension(file.FileName).ToLower();

                    if (ext == ".zip")
                    {
                        var extractPath = Path.Combine(tempDirectory, Guid.NewGuid().ToString());
                        Directory.CreateDirectory(extractPath);
                        ZipFile.ExtractToDirectory(tempFilePath, extractPath);

                        var notebookFilesInZip = Directory.GetFiles(extractPath, "*.*", SearchOption.AllDirectories)
                            .Where(f => f.EndsWith(".ipynb", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".py", StringComparison.OrdinalIgnoreCase));

                        foreach (var notebookPath in notebookFilesInZip)
                        {
                            string fileContent = await File.ReadAllTextAsync(notebookPath);
                            string fileExt = Path.GetExtension(notebookPath).ToLower();
                            filesToProcess.Add((fileContent, fileExt));
                        }
                    }
                    else if (ext == ".ipynb" || ext == ".py")
                    {
                        string fileContent = await File.ReadAllTextAsync(tempFilePath);
                        filesToProcess.Add((fileContent, ext));
                    }
                }

                var tempTableNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var fileItem in filesToProcess)
                {
                    if (fileItem.Extension == ".ipynb")
                    {
                        try
                        {
                            var notebook = JsonSerializer.Deserialize<Notebook>(fileItem.Content, new JsonSerializerOptions { AllowTrailingCommas = true });
                            if (notebook?.Cells != null)
                            {
                                foreach (var cell in notebook.Cells)
                                {
                                    if (cell.CellType == "code")
                                    {
                                        var code = string.Join(" ", cell.Source);
                                        ParseCodeContent(code, tempTableNames);
                                    }
                                }
                            }
                        }
                        catch
                        {
                            ParseCodeContent(fileItem.Content, tempTableNames);
                        }
                    }
                    else
                    {
                        ParseCodeContent(fileItem.Content, tempTableNames);
                    }
                }

                var tablesList = tempTableNames.OrderBy(t => t).ToList();
                var notebookContent = BuildOutputNotebook(tablesList, baseFileName, platinumTempDb, dbLocationPlatinumTemp);

                return (notebookContent, tablesList);
            }
            finally
            {
                if (Directory.Exists(tempDirectory)) Directory.Delete(tempDirectory, true);
            }
        }

        private void ParseCodeContent(string code, HashSet<string> tempTableNames)
        {
            var matches = CreateTableWithDbVarRegex.Matches(code);
            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                    tempTableNames.Add(match.Groups[1].Value);
            }
        }

        private string BuildOutputNotebook(List<string> tableNames, string baseFileName, string platinumTempDb, string dbLocationPlatinumTemp)
        {
            var notebook = new { cells = new List<object>(), metadata = new { }, nbformat = 4, nbformat_minor = 0 };
            notebook.cells.Add(CreateMarkdownCell($"# {baseFileName}"));
            notebook.cells.Add(CreateMarkdownCell("## Captura de Variables"));
            notebook.cells.Add(CreateCodeCell($"dbutils.widgets.removeAll()\ndbutils.widgets.text(\"platinum_temp_dbW\", \"{platinumTempDb}\", \"01 DB Platinum Temp:\")\ndbutils.widgets.text(\"db_location_platinum_tempW\", \"{dbLocationPlatinumTemp}\", \"02 Location Platinum Temp DB:\")"));
            notebook.cells.Add(CreateCodeCell("platinum_temp_dbX = dbutils.widgets.get(\"platinum_temp_dbW\")\nspark.conf.set(\"bci.platinum_temp_dbX\", platinum_temp_dbX)\ndb_location_platinum_temp_X = dbutils.widgets.get(\"db_location_platinum_tempW\")\nspark.conf.set(\"bci.db_location_platinum_temp_X\", db_location_platinum_temp_X)"));
            notebook.cells.Add(CreateMarkdownCell("## Funciones"));
            notebook.cells.Add(CreateCodeCell("def sql_safe(query):\n  try:\n    print (\"sqlSafe: query -> \" + query)\n    return spark.sql(query)\n  except Exception as e:\n    dbutils.notebook.exit(\"{\\\"coderror\\\":\\\"20001\\\", \\\"msgerror\\\":\\\"Error grave procesado consulta -> \"+str(e)+\"\\\"}\")"));

            foreach (var tableName in tableNames)
            {
                var safeName = new string(tableName.Where(char.IsLetterOrDigit).ToArray());
                var varName = $"paso_tb_del_{safeName.Substring(0, Math.Min(safeName.Length, 15))}";
                notebook.cells.Add(CreateMarkdownCell($"### Tabla {tableName}"));
                notebook.cells.Add(CreateCodeCell($"{varName} = \"\"\"DROP TABLE IF EXISTS \"\"\" + platinum_temp_dbX + \".{tableName}\"\"\"\nsql_safe({varName})\ndbutils.fs.rm(db_location_platinum_temp_X+\"{tableName}\", True)"));
            }

            notebook.cells.Add(CreateMarkdownCell("## Mensaje Final"));
            notebook.cells.Add(CreateCodeCell("dbutils.notebook.exit(\"{\\\"coderror\\\":\\\"0\\\", \\\"msgerror\\\":\\\"Notebook termina ejecucion satisfactoriamente\\\"}\")"));

            return JsonSerializer.Serialize(notebook, new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
        }

        private object CreateMarkdownCell(string source) => new { cell_type = "markdown", metadata = new { }, source = source.Split('\n') };

        // CORRECCIÓN: Se removió el punto y coma erróneo antes de la llave de cierre
        private object CreateCodeCell(string source) => new { cell_type = "code", execution_count = (object)null, metadata = new { }, outputs = new List<object>(), source = source.Split('\n') };
    }
}
