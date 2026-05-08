using ExcelDataReader;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NotebookValidator.Web.Models;

namespace NotebookValidator.Web.Services
{
    public class DataProfilingService
    {
        // Expresiones regulares para PII Estándar
        private static readonly Regex EmailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex RutCompletoRegex = new Regex(@"^\d{1,2}\.?\d{3}\.?\d{3}[-][0-9kK]{1}$", RegexOptions.Compiled);
        private static readonly Regex CreditCardRegex = new Regex(@"^\d{4}[ -]?\d{4}[ -]?\d{4}[ -]?\d{4}$", RegexOptions.Compiled);

        // Regex para detectar nombres de columnas de RUT dividido
        private static readonly Regex RutNameRegex = new Regex(@"(^|_)(rut|run)($|_)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex DvNameRegex = new Regex(@"(^|_)(dv|digito_verificador)($|_)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex DvValueRegex = new Regex(@"^[0-9kK]$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public DataProfileResult AnalyzeFile(IFormFile file, bool hasHeaders)
        {
            var result = new DataProfileResult { FileName = file.FileName };

            using var stream = file.OpenReadStream();
            string extension = Path.GetExtension(file.FileName).ToLower();
            bool isFlatFile = extension == ".csv" || extension == ".txt" || extension == ".dat";
            IExcelDataReader reader;

            if (isFlatFile)
            {
                var configCsv = new ExcelReaderConfiguration() { AutodetectSeparators = new char[] { ',', ';', '\t', '|' } };
                reader = ExcelReaderFactory.CreateCsvReader(stream, configCsv);
            }
            else
            {
                reader = ExcelReaderFactory.CreateReader(stream);
            }

            var ds = reader.AsDataSet(new ExcelDataSetConfiguration() { ConfigureDataTable = (_) => new ExcelDataTableConfiguration() { UseHeaderRow = hasHeaders } });
            var dt = ds.Tables.Count > 0 ? ds.Tables[0] : new DataTable();

            if (!hasHeaders && dt.Columns.Count > 0)
                for (int i = 0; i < dt.Columns.Count; i++) dt.Columns[i].ColumnName = $"Columna_{i + 1}";

            result.TotalRows = dt.Rows.Count;
            result.TotalColumns = dt.Columns.Count;

            if (result.TotalRows == 0) return result;

            foreach (DataColumn column in dt.Columns)
            {
                result.ColumnProfiles.Add(AnalyzeColumn(dt, column, result.TotalRows));
            }

            return result;
        }

        private ColumnProfile AnalyzeColumn(DataTable dt, DataColumn column, int totalRows)
        {
            var profile = new ColumnProfile { ColumnName = column.ColumnName };
            var values = new List<string>();
            var numericValues = new List<double>();
            int nullCount = 0, zeroCount = 0, negativeCount = 0, whitespaceCount = 0;

            // --- NUEVO: Bandera para detectar si hay decimales en la columna ---
            bool hasDecimals = false;

            foreach (DataRow row in dt.Rows)
            {
                var rawVal = row[column]?.ToString();

                if (string.IsNullOrEmpty(rawVal))
                {
                    nullCount++;
                }
                else if (string.IsNullOrWhiteSpace(rawVal))
                {
                    nullCount++;
                    whitespaceCount++;
                }
                else
                {
                    var val = rawVal.Trim();
                    values.Add(val);
                    if (double.TryParse(val.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double num))
                    {
                        numericValues.Add(num);
                        if (num == 0) zeroCount++;
                        else if (num < 0) negativeCount++;

                        // Si el residuo de dividir por 1 no es 0, significa que tiene parte decimal (ej. 3.14 % 1 = 0.14)
                        if (Math.Abs(num % 1) > double.Epsilon)
                        {
                            hasDecimals = true;
                        }
                    }
                }
            }

            profile.NullOrEmptyCount = nullCount;
            profile.NullPercentage = Math.Round((double)nullCount / totalRows * 100, 2);
            profile.WhitespaceCount = whitespaceCount;
            profile.ZeroCount = zeroCount;
            profile.NegativeCount = negativeCount;

            var uniqueValues = values.GroupBy(v => v).ToList();
            profile.UniqueValuesCount = uniqueValues.Count;
            profile.UniquePercentage = values.Any() ? Math.Round((double)uniqueValues.Count / values.Count * 100, 2) : 0;

            if (profile.NullPercentage > 30 || (profile.UniqueValuesCount <= 1 && values.Any())) profile.HealthStatus = "Crítica";
            else if (profile.NullPercentage > 5) profile.HealthStatus = "Advertencia";
            else profile.HealthStatus = "Óptima";

            // DETECCIÓN DE PII
            if (uniqueValues.Any())
            {
                int emailHits = 0, rutHits = 0, ccHits = 0;
                int mantisaHits = 0, dvHits = 0;

                var sample = uniqueValues.Take(500).Select(g => g.Key).ToList();

                bool isRutName = RutNameRegex.IsMatch(profile.ColumnName);
                bool isDvName = DvNameRegex.IsMatch(profile.ColumnName);

                foreach (var val in sample)
                {
                    if (RutCompletoRegex.IsMatch(val)) rutHits++;
                    else if (EmailRegex.IsMatch(val)) emailHits++;
                    else if (CreditCardRegex.IsMatch(val)) ccHits++;
                    else
                    {
                        if (isRutName && double.TryParse(val, out double num) && num >= 100000 && num <= 99999999) mantisaHits++;
                        if (isDvName && DvValueRegex.IsMatch(val)) dvHits++;
                    }
                }

                if (ccHits > 0) { profile.IsPII = true; profile.PIIType = "Tarjeta de Crédito"; }
                else if (rutHits > sample.Count * 0.10) { profile.IsPII = true; profile.PIIType = "RUT Chileno Completo"; }
                else if (emailHits > sample.Count * 0.10) { profile.IsPII = true; profile.PIIType = "Email"; }
                else if (isRutName && mantisaHits > sample.Count * 0.50) { profile.IsPII = true; profile.PIIType = "RUT (Mantisa)"; }
                else if (isDvName && dvHits > sample.Count * 0.50) { profile.IsPII = true; profile.PIIType = "RUT (Dígito Verificador)"; }
            }

            profile.TopValues = uniqueValues.OrderByDescending(g => g.Count()).Take(3).Select(g => new TopValueItem
            {
                Value = string.IsNullOrEmpty(g.Key) ? "(Vacío)" : g.Key,
                Count = g.Count(),
                Percentage = values.Any() ? Math.Round((double)g.Count() / values.Count * 100, 2) : 0
            }).ToList();

            // --- NUEVO: DIFERENCIACIÓN ENTRE ENTERO Y DECIMAL ---
            if (values.Count > 0 && numericValues.Count == values.Count && !profile.IsPII)
            {
                profile.InferredDataType = hasDecimals ? "Decimal" : "Entero";

                // Si es decimal le damos 4 ceros de precisión (N4), si es entero le quitamos los decimales (N0)
                string format = hasDecimals ? "N4" : "N0";

                profile.MinValue = numericValues.Min().ToString(format, CultureInfo.InvariantCulture);
                profile.MaxValue = numericValues.Max().ToString(format, CultureInfo.InvariantCulture);
                profile.Average = Math.Round(numericValues.Average(), hasDecimals ? 4 : 2);
            }
            else if (values.Count > 0 && values.All(v => DateTime.TryParse(v, out _)))
            {
                profile.InferredDataType = "Fecha";
                var dates = values.Select(v => DateTime.Parse(v)).ToList();
                profile.MinValue = dates.Min().ToString("yyyy-MM-dd");
                profile.MaxValue = dates.Max().ToString("yyyy-MM-dd");
            }
            else
            {
                profile.InferredDataType = "Texto";
                if (values.Any())
                {
                    profile.MinValue = "Len: " + values.Min(v => v.Length).ToString();
                    profile.MaxValue = "Len: " + values.Max(v => v.Length).ToString();
                }
            }

            return profile;
        }
    }
}
