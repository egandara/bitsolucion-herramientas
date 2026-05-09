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

        private static readonly Regex RutNameRegex = new Regex(@"(^|_)(rut|run)($|_)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex DvNameRegex = new Regex(@"(^|_)(dv|digito_verificador)($|_)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex DvValueRegex = new Regex(@"^[0-9kK]$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public DataProfileResult AnalyzeFile(IFormFile file, bool hasHeaders)
        {
            using var stream = file.OpenReadStream();
            return AnalyzeFile(stream, file.FileName, hasHeaders);
        }

        public DataProfileResult AnalyzeFile(Stream stream, string fileName, bool hasHeaders)
        {
            var result = new DataProfileResult { FileName = fileName, HasHeaders = hasHeaders };

            string extension = Path.GetExtension(fileName).ToLower();
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

        private bool TryParseLenientNumber(string s, out double value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(s)) return false;

            var v = s.Trim();
            v = v.Replace("\u00A0", "");
            v = v.Replace(" ", "");
            v = v.Replace("$", "").Replace("€", "").Replace("£", "");
            v = v.Replace("\"", "").Replace("'", "");

            if (Regex.IsMatch(v, @"[A-Za-z\u00C0-\u017F]"))
            {
                var parenMatch = Regex.Match(v, @"^\((.*)\)$");
                if (parenMatch.Success)
                {
                    var inner = parenMatch.Groups[1].Value;
                    if (TryParseLenientNumber(inner, out double innerVal))
                    {
                        value = -innerVal;
                        return true;
                    }
                }
                return false;
            }

            int countDot = v.Count(c => c == '.');
            int countComma = v.Count(c => c == ',');

            string candidate = v;

            try
            {
                if (countDot > 0 && countComma > 0)
                {
                    if (v.LastIndexOf(',') > v.LastIndexOf('.'))
                    {
                        candidate = v.Replace(".", "").Replace(",", ".");
                    }
                    else
                    {
                        candidate = v.Replace(",", "");
                    }
                }
                else if (countComma > 0 && countDot == 0)
                {
                    if (countComma == 1 && Regex.IsMatch(v, @",\d{1,3}$"))
                    {
                        candidate = v.Replace(",", ".");
                    }
                    else
                    {
                        candidate = v.Replace(",", "");
                    }
                }
                else if (countDot > 0 && countComma == 0)
                {
                    if (countDot == 1 && Regex.IsMatch(v, @"\.\d{1,3}$"))
                    {
                        candidate = v;
                    }
                    else
                    {
                        candidate = v.Replace(".", "");
                    }
                }

                if (double.TryParse(candidate, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
                {
                    return true;
                }

                var alt = candidate.Replace(",", ".");
                if (double.TryParse(alt, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        public ColumnValidationResult ValidateTextColumn(string filePath, bool hasHeaders, string columnName, int sampleLimit = 5000, int mismatchSampleLimit = 100, string targetType = null)
        {
            using var stream = File.OpenRead(filePath);
            string extension = Path.GetExtension(filePath).ToLower();
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

            var result = new ColumnValidationResult { ColumnName = columnName, TotalSampled = 0 };

            if (!dt.Columns.Contains(columnName)) return result;

            var col = dt.Columns[columnName];
            int intCount = 0, decCount = 0, dateCount = 0, textCount = 0;
            var mismatches = new List<MismatchRow>();
            var sampleValues = new List<string>();

            int rows = Math.Min(dt.Rows.Count, sampleLimit);
            for (int i = 0; i < rows; i++)
            {
                var raw = dt.Rows[i][col]?.ToString();
                if (sampleValues.Count < 20)
                {
                    sampleValues.Add(raw ?? string.Empty);
                }

                if (string.IsNullOrEmpty(raw) || string.IsNullOrWhiteSpace(raw))
                {
                    textCount++;
                    continue;
                }

                var v = raw.Trim();

                if (DateTime.TryParse(v, out _))
                {
                    dateCount++;
                }
                else if (TryParseLenientNumber(v, out double num))
                {
                    if (Math.Abs(num % 1) > double.Epsilon)
                    {
                        decCount++;
                    }
                    else
                    {
                        intCount++;
                    }
                }
                else
                {
                    textCount++;
                }

                // SI SE PIDE UN TARGET TYPE, BUSCAMOS LOS QUE "NO" COINCIDEN CON ÉL
                if (!string.IsNullOrEmpty(targetType) && mismatches.Count < mismatchSampleLimit)
                {
                    bool fails = false;
                    string reason = string.Empty;

                    if (targetType == "Entero")
                    {
                        // Falla si no se puede convertir a número, o si el residuo decimal no es 0
                        if (!TryParseLenientNumber(v, out double d) || Math.Abs(d % 1) > double.Epsilon)
                        {
                            fails = true;
                            reason = "No es un número entero";
                        }
                    }
                    else if (targetType == "Decimal")
                    {
                        // Falla si no se puede parsear como número de ninguna forma
                        if (!TryParseLenientNumber(v, out _))
                        {
                            fails = true;
                            reason = "No es un valor numérico";
                        }
                    }
                    else if (targetType == "Fecha")
                    {
                        // Falla si no se puede parsear como fecha
                        if (!DateTime.TryParse(v, out _))
                        {
                            fails = true;
                            reason = "Formato de fecha inválido";
                        }
                    }

                    if (fails)
                    {
                        mismatches.Add(new MismatchRow { RowIndex = i + 1, Value = v, Reason = reason });
                    }
                }
            }

            int total = intCount + decCount + dateCount + textCount;
            result.TotalSampled = total;
            result.IntegerCount = intCount;
            result.DecimalCount = decCount;
            result.DateCount = dateCount;
            result.TextCount = textCount;

            result.IntegerPercentage = total > 0 ? Math.Round((double)intCount / total * 100, 2) : 0;
            result.DecimalPercentage = total > 0 ? Math.Round((double)decCount / total * 100, 2) : 0;
            result.DatePercentage = total > 0 ? Math.Round((double)dateCount / total * 100, 2) : 0;
            result.TextPercentage = total > 0 ? Math.Round((double)textCount / total * 100, 2) : 0;

            result.IsLikelyNumeric = (result.IntegerPercentage + result.DecimalPercentage) >= 90.0;
            if (result.IsLikelyNumeric)
            {
                result.Recommendation = "Alto porcentaje numérico — considerar convertir la columna a Numérico.";
            }
            else if ((result.IntegerPercentage + result.DecimalPercentage) >= 80.0)
            {
                result.Recommendation = "Porcentaje numérico moderado — revisar los valores que no coinciden antes de convertir.";
            }
            else
            {
                result.Recommendation = "Predomina Texto — no se recomienda conversión automática.";
            }

            result.MismatchRows = mismatches;
            result.SampleValues = sampleValues;

            return result;
        }

        public List<DuplicateGroup> FindDuplicateGroups(string filePath, bool hasHeaders, string[] keyColumns, bool fullRow)
        {
            using var stream = File.OpenRead(filePath);
            string extension = Path.GetExtension(filePath).ToLower();
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

            var result = new List<DuplicateGroup>();

            if (dt.Rows.Count == 0) return result;

            List<int> compareIndexes;
            if (fullRow || keyColumns == null || keyColumns.Length == 0)
            {
                compareIndexes = Enumerable.Range(0, dt.Columns.Count).ToList();
            }
            else
            {
                compareIndexes = new List<int>();
                foreach (var colName in keyColumns)
                {
                    var found = dt.Columns.Cast<DataColumn>().FirstOrDefault(c => string.Equals(c.ColumnName, colName, StringComparison.OrdinalIgnoreCase));
                    if (found != null) compareIndexes.Add(found.Ordinal);
                }

                if (!compareIndexes.Any()) compareIndexes = Enumerable.Range(0, dt.Columns.Count).ToList();
            }

            var groups = new Dictionary<string, DuplicateGroup>(StringComparer.Ordinal);
            for (int rowIndex = 0; rowIndex < dt.Rows.Count; rowIndex++)
            {
                var row = dt.Rows[rowIndex];
                var parts = compareIndexes.Select(i => row[i]?.ToString() ?? string.Empty).ToArray();
                var key = string.Join("\u001F", parts);

                if (!groups.TryGetValue(key, out DuplicateGroup group))
                {
                    group = new DuplicateGroup
                    {
                        Key = key,
                        Count = 0,
                        RowIndices = new List<int>(),
                        SampleDisplay = string.Join(" | ", parts.Select(p => string.IsNullOrEmpty(p) ? "(vacío)" : p))
                    };
                    groups[key] = group;
                }

                group.Count++;
                group.RowIndices.Add(rowIndex + 1);
            }

            result = groups.Values.Where(g => g.Count > 1).OrderByDescending(g => g.Count).ToList();
            return result;
        }

        private ColumnProfile AnalyzeColumn(DataTable dt, DataColumn column, int totalRows)
        {
            var profile = new ColumnProfile { ColumnName = column.ColumnName };
            var values = new List<string>();
            var numericValues = new List<double>();
            int nullCount = 0, zeroCount = 0, negativeCount = 0, whitespaceCount = 0;

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

            if (values.Count > 0 && numericValues.Count == values.Count && !profile.IsPII)
            {
                profile.InferredDataType = hasDecimals ? "Decimal" : "Entero";
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
