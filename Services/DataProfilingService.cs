using ExcelDataReader;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NotebookValidator.Web.Models;

namespace NotebookValidator.Web.Services
{
    public class DataProfilingService
    {
        private static readonly Regex EmailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex RutCompletoRegex = new Regex(@"^\d{1,2}\.?\d{3}\.?\d{3}[-][0-9kK]{1}$", RegexOptions.Compiled);
        private static readonly Regex CreditCardRegex = new Regex(@"^\d{4}[ -]?\d{4}[ -]?\d{4}[ -]?\d{4}$", RegexOptions.Compiled);
        private static readonly Regex RutNameRegex = new Regex(@"(^|_)(rut|run)($|_)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex DvNameRegex = new Regex(@"(^|_)(dv|digito_verificador)($|_)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex DvValueRegex = new Regex(@"^[0-9kK]$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private class ColumnTracker
        {
            public int NullCount = 0;
            public int WhitespaceCount = 0;
            public int ZeroCount = 0;
            public int NegativeCount = 0;
            public double MinNum = double.MaxValue;
            public double MaxNum = double.MinValue;
            public double SumNum = 0;

            // Separamos enteros y decimales
            public int IntegerCount = 0;
            public int DecimalCount = 0;
            public int DateCount = 0;
            public int TotalCount = 0;

            public int MinLength = int.MaxValue;
            public int MaxLength = int.MinValue;
            public bool HasDecimals = false;
            public DateTime MinDate = DateTime.MaxValue;
            public DateTime MaxDate = DateTime.MinValue;

            public Dictionary<string, int> Frequencies = new Dictionary<string, int>(StringComparer.Ordinal);
            private const int MAX_UNIQUE = 15000;
            public bool UniqueCapped = false;

            public void ProcessValue(string rawVal)
            {
                TotalCount++;
                if (string.IsNullOrEmpty(rawVal)) { NullCount++; return; }
                if (string.IsNullOrWhiteSpace(rawVal)) { NullCount++; WhitespaceCount++; return; }

                string val = rawVal.Trim();

                if (val.Length < MinLength) MinLength = val.Length;
                if (val.Length > MaxLength) MaxLength = val.Length;

                if (!UniqueCapped)
                {
                    if (Frequencies.TryGetValue(val, out int c)) { Frequencies[val] = c + 1; }
                    else
                    {
                        if (Frequencies.Count < MAX_UNIQUE) Frequencies.Add(val, 1);
                        else UniqueCapped = true;
                    }
                }

                // Detección inteligente de fechas
                bool isDate = false;
                DateTime parsedDate = DateTime.MinValue;

                // 1. Detectar formato compacto yyyyMMdd (ej. 20260510)
                if (val.Length == 8 && Regex.IsMatch(val, @"^\d{8}$") && DateTime.TryParseExact(val, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate))
                {
                    if (parsedDate.Year >= 1900 && parsedDate.Year <= 2100) isDate = true;
                }
                // 1.1 Detectar formato periodo yyyyMM (ej. 202605)
                else if (val.Length == 6 && Regex.IsMatch(val, @"^\d{6}$") && DateTime.TryParseExact(val, "yyyyMM", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate))
                {
                    if (parsedDate.Year >= 1900 && parsedDate.Year <= 2100) isDate = true;
                }
                // 2. Detección de fechas estándar (ej. 2026-05-10, 10/05/2026)
                else if (DateTime.TryParse(val, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate))
                {
                    if (!double.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                    {
                        isDate = true;
                    }
                    else if (val.Contains("-") || val.Contains("/"))
                    {
                        isDate = true;
                    }
                }

                if (isDate)
                {
                    DateCount++;
                    if (parsedDate < MinDate) MinDate = parsedDate;
                    if (parsedDate > MaxDate) MaxDate = parsedDate;
                }
                else if (double.TryParse(val.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double num))
                {
                    if (Math.Abs(num % 1) > double.Epsilon) { DecimalCount++; HasDecimals = true; }
                    else { IntegerCount++; }

                    SumNum += num;
                    if (num == 0) ZeroCount++;
                    else if (num < 0) NegativeCount++;
                    if (num < MinNum) MinNum = num;
                    if (num > MaxNum) MaxNum = num;
                }
            }
        }

        public DataProfileResult AnalyzeFile(IFormFile file, bool hasHeaders)
        {
            using var stream = file.OpenReadStream();
            return AnalyzeFile(stream, file.FileName, hasHeaders);
        }

        public DataProfileResult AnalyzeFile(Stream stream, string fileName, bool hasHeaders)
        {
            var result = new DataProfileResult { FileName = fileName, HasHeaders = hasHeaders };
            string extension = Path.GetExtension(fileName).ToLower();
            result.FileExtension = string.IsNullOrEmpty(extension) ? "DESCONOCIDO" : extension.ToUpper().Replace(".", "");

            bool isFlatFile = extension == ".csv" || extension == ".txt" || extension == ".dat";

            if (isFlatFile)
            {
                var sniff = SniffFile(stream);
                result.SeparatorUsed = sniff.separator;
                result.LineEndingType = sniff.lineEnding;
            }
            else
            {
                result.LineEndingType = "Windows (CRLF)";
                result.SeparatorUsed = "Excel Internal";
            }

            stream.Position = 0;

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

            int fieldCount = reader.FieldCount;
            if (fieldCount == 0) return result;

            var columnNames = new string[fieldCount];
            var trackers = new ColumnTracker[fieldCount];

            if (hasHeaders && reader.Read())
            {
                for (int i = 0; i < fieldCount; i++)
                {
                    columnNames[i] = reader.GetValue(i)?.ToString() ?? $"Columna_{i + 1}";
                    trackers[i] = new ColumnTracker();
                }
            }
            else
            {
                for (int i = 0; i < fieldCount; i++)
                {
                    columnNames[i] = $"Columna_{i + 1}";
                    trackers[i] = new ColumnTracker();
                }
            }

            int rowCount = 0;
            while (reader.Read())
            {
                rowCount++;
                for (int i = 0; i < fieldCount; i++)
                {
                    trackers[i].ProcessValue(reader.GetValue(i)?.ToString());
                }
            }

            result.TotalRows = rowCount;
            result.TotalColumns = fieldCount;

            if (rowCount == 0) return result;

            var profilesArray = new ColumnProfile[fieldCount];
            Parallel.For(0, fieldCount, i =>
            {
                profilesArray[i] = FinalizeProfile(columnNames[i], trackers[i], rowCount);
            });

            result.ColumnProfiles = profilesArray.ToList();
            return result;
        }

        private (string separator, string lineEnding) SniffFile(Stream stream)
        {
            long pos = stream.Position;
            stream.Position = 0;
            using var reader = new StreamReader(stream, leaveOpen: true);

            char[] buffer = new char[524288];
            int read = reader.ReadBlock(buffer, 0, buffer.Length);
            string chunk = new string(buffer, 0, read);
            stream.Position = pos;

            string lineEnding = chunk.Contains("\r\n") ? "Windows (CRLF)" : (chunk.Contains("\n") ? "Unix (LF)" : "Desconocido");

            char[] candidates = { ',', ';', '\t', '|' };
            string firstLine = chunk.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).FirstOrDefault() ?? "";

            char bestSep = ',';
            int maxCount = -1;

            foreach (var c in candidates)
            {
                int count = firstLine.Count(x => x == c);
                if (count > maxCount) { maxCount = count; bestSep = c; }
            }

            string sepName = bestSep == '\t' ? "Tabulación" : bestSep.ToString();
            return (sepName, lineEnding);
        }

        private ColumnProfile FinalizeProfile(string columnName, ColumnTracker tracker, int totalRows)
        {
            var profile = new ColumnProfile { ColumnName = columnName };

            profile.NullOrEmptyCount = tracker.NullCount;
            profile.NullPercentage = Math.Round((double)tracker.NullCount / totalRows * 100, 2);
            profile.WhitespaceCount = tracker.WhitespaceCount;
            profile.ZeroCount = tracker.ZeroCount;
            profile.NegativeCount = tracker.NegativeCount;

            int distinctTracked = tracker.Frequencies.Count;
            profile.UniqueValuesCount = tracker.UniqueCapped ? (distinctTracked + 1) : distinctTracked;
            int validValuesCount = totalRows - tracker.NullCount;
            profile.UniquePercentage = validValuesCount > 0 ? Math.Round((double)profile.UniqueValuesCount / validValuesCount * 100, 2) : 0;

            if (profile.NullPercentage > 30 || (profile.UniqueValuesCount <= 1 && validValuesCount > 0)) profile.HealthStatus = "Crítica";
            else if (profile.NullPercentage > 5) profile.HealthStatus = "Advertencia";
            else profile.HealthStatus = "Óptima";

            profile.DetectedIntegerCount = tracker.IntegerCount;
            profile.DetectedDecimalCount = tracker.DecimalCount;
            profile.DetectedDateCount = tracker.DateCount;
            profile.DetectedTextCount = validValuesCount - tracker.IntegerCount - tracker.DecimalCount - tracker.DateCount;
            if (profile.DetectedTextCount < 0) profile.DetectedTextCount = 0;

            if (validValuesCount > 0)
            {
                double numPct = ((double)(tracker.IntegerCount + tracker.DecimalCount) / validValuesCount) * 100;
                if (numPct >= 90.0) profile.ValidationRecommendation = "Alto porcentaje numérico — considerar conversión automática.";
                else if (numPct >= 80.0) profile.ValidationRecommendation = "Porcentaje numérico moderado — revisar posibles conflictos antes de convertir.";
                else profile.ValidationRecommendation = "Predomina Texto — no se recomienda conversión automática.";
            }

            if (distinctTracked > 0)
            {
                int emailHits = 0, rutHits = 0, ccHits = 0, mantisaHits = 0, dvHits = 0;
                var sample = tracker.Frequencies.Keys.Take(500).ToList();

                bool isRutName = RutNameRegex.IsMatch(profile.ColumnName);
                bool isDvName = DvNameRegex.IsMatch(profile.ColumnName);

                foreach (var val in sample)
                {
                    bool hasAt = val.Contains("@");
                    bool hasDash = val.Contains("-");

                    if (hasDash && val.Length >= 8 && val.Length <= 12 && RutCompletoRegex.IsMatch(val)) rutHits++;
                    else if (hasAt && EmailRegex.IsMatch(val)) emailHits++;
                    else if (val.Length >= 15 && val.Length <= 19 && (val.Contains(" ") || hasDash) && CreditCardRegex.IsMatch(val)) ccHits++;
                    else
                    {
                        if (isRutName && val.Length >= 6 && val.Length <= 8 && double.TryParse(val, out double num) && num >= 100000 && num <= 99999999) mantisaHits++;
                        if (isDvName && val.Length == 1 && DvValueRegex.IsMatch(val)) dvHits++;
                    }
                }

                if (ccHits > 0) { profile.IsPII = true; profile.PIIType = "Tarjeta de Crédito"; }
                else if (rutHits > sample.Count * 0.10) { profile.IsPII = true; profile.PIIType = "RUT Chileno Completo"; }
                else if (emailHits > sample.Count * 0.10) { profile.IsPII = true; profile.PIIType = "Email"; }
                else if (isRutName && mantisaHits > sample.Count * 0.50) { profile.IsPII = true; profile.PIIType = "RUT (Mantisa)"; }
                else if (isDvName && dvHits > sample.Count * 0.50) { profile.IsPII = true; profile.PIIType = "RUT (Dígito Verificador)"; }
            }

            profile.TopValues = tracker.Frequencies.OrderByDescending(kvp => kvp.Value).Take(3).Select(kvp => new TopValueItem
            {
                Value = string.IsNullOrEmpty(kvp.Key) ? "(Vacío)" : kvp.Key,
                Count = kvp.Value,
                Percentage = validValuesCount > 0 ? Math.Round((double)kvp.Value / validValuesCount * 100, 2) : 0
            }).ToList();

            int numericTotal = tracker.IntegerCount + tracker.DecimalCount;

            if (validValuesCount > 0 && tracker.DateCount > numericTotal && tracker.DateCount >= profile.DetectedTextCount)
            {
                profile.InferredDataType = "Fecha";
                profile.MinValue = tracker.MinDate != DateTime.MaxValue ? tracker.MinDate.ToString("yyyy-MM-dd") : "-";
                profile.MaxValue = tracker.MaxDate != DateTime.MinValue ? tracker.MaxDate.ToString("yyyy-MM-dd") : "-";
                profile.Average = null;
            }
            else if (validValuesCount > 0 && numericTotal > tracker.DateCount && numericTotal >= profile.DetectedTextCount && !profile.IsPII)
            {
                profile.InferredDataType = tracker.HasDecimals ? "Decimal" : "Entero";
                string format = tracker.HasDecimals ? "N4" : "N0";

                profile.MinValue = tracker.MinNum != double.MaxValue ? tracker.MinNum.ToString(format, CultureInfo.InvariantCulture) : "-";
                profile.MaxValue = tracker.MaxNum != double.MinValue ? tracker.MaxNum.ToString(format, CultureInfo.InvariantCulture) : "-";
                profile.Average = numericTotal > 0 ? Math.Round(tracker.SumNum / numericTotal, tracker.HasDecimals ? 4 : 2) : null;
            }
            else
            {
                profile.InferredDataType = "Texto";
                if (validValuesCount > 0)
                {
                    profile.MinValue = "Len: " + (tracker.MinLength == int.MaxValue ? 0 : tracker.MinLength).ToString();
                    profile.MaxValue = "Len: " + (tracker.MaxLength == int.MinValue ? 0 : tracker.MaxLength).ToString();
                }
            }

            return profile;
        }

        private bool TryParseLenientNumber(string s, out double value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(s)) return false;

            var v = s.Trim().Replace("\u00A0", "").Replace(" ", "").Replace("$", "").Replace("€", "").Replace("£", "").Replace("\"", "").Replace("'", "");

            if (Regex.IsMatch(v, @"[A-Za-z\u00C0-\u017F]"))
            {
                var parenMatch = Regex.Match(v, @"^\((.*)\)$");
                if (parenMatch.Success && TryParseLenientNumber(parenMatch.Groups[1].Value, out double innerVal))
                {
                    value = -innerVal;
                    return true;
                }
                return false;
            }

            int countDot = v.Count(c => c == '.');
            int countComma = v.Count(c => c == ',');
            string candidate = v;

            try
            {
                if (countDot > 0 && countComma > 0) candidate = v.LastIndexOf(',') > v.LastIndexOf('.') ? v.Replace(".", "").Replace(",", ".") : v.Replace(",", "");
                else if (countComma > 0 && countDot == 0) candidate = (countComma == 1 && Regex.IsMatch(v, @",\d{1,3}$")) ? v.Replace(",", ".") : v.Replace(",", "");
                else if (countDot > 0 && countComma == 0) candidate = (countDot == 1 && Regex.IsMatch(v, @"\.\d{1,3}$")) ? v : v.Replace(".", "");

                if (double.TryParse(candidate, NumberStyles.Any, CultureInfo.InvariantCulture, out value)) return true;
                if (double.TryParse(candidate.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out value)) return true;
            }
            catch { return false; }

            return false;
        }

        public ColumnValidationResult ValidateTextColumn(string filePath, bool hasHeaders, string columnName, int sampleLimit = 5000, int mismatchSampleLimit = 100, string targetType = null)
        {
            var result = new ColumnValidationResult { ColumnName = columnName, TotalSampled = 0 };

            using var stream = File.OpenRead(filePath);
            string extension = Path.GetExtension(filePath).ToLower();
            bool isFlatFile = extension == ".csv" || extension == ".txt" || extension == ".dat";

            var configCsv = new ExcelReaderConfiguration() { AutodetectSeparators = new char[] { ',', ';', '\t', '|' } };
            using var reader = isFlatFile ? ExcelReaderFactory.CreateCsvReader(stream, configCsv) : ExcelReaderFactory.CreateReader(stream);

            int targetColumnIndex = -1;

            if (hasHeaders && reader.Read())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    if (string.Equals(reader.GetValue(i)?.ToString() ?? $"Columna_{i + 1}", columnName, StringComparison.OrdinalIgnoreCase))
                    {
                        targetColumnIndex = i;
                        break;
                    }
                }
            }
            else
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    if ($"Columna_{i + 1}" == columnName) { targetColumnIndex = i; break; }
                }
            }

            if (targetColumnIndex == -1) return result;

            int intCount = 0, decCount = 0, dateCount = 0, textCount = 0;
            var mismatches = new List<MismatchRow>();
            var sampleValues = new List<string>();

            int count = 0;
            while (reader.Read() && count < sampleLimit)
            {
                count++;
                var raw = reader.GetValue(targetColumnIndex)?.ToString();

                if (sampleValues.Count < 20) sampleValues.Add(raw ?? string.Empty);

                if (string.IsNullOrWhiteSpace(raw)) { textCount++; continue; }

                var v = raw.Trim();

                // Detección consistente para Validación de columnas (yyyyMMdd y yyyyMM)
                if (v.Length == 8 && Regex.IsMatch(v, @"^\d{8}$") && DateTime.TryParseExact(v, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dtOut) && dtOut.Year >= 1900 && dtOut.Year <= 2100) dateCount++;
                else if (v.Length == 6 && Regex.IsMatch(v, @"^\d{6}$") && DateTime.TryParseExact(v, "yyyyMM", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dtOut6) && dtOut6.Year >= 1900 && dtOut6.Year <= 2100) dateCount++;
                else if (DateTime.TryParse(v, CultureInfo.InvariantCulture, DateTimeStyles.None, out _) && (!double.TryParse(v, out _) || v.Contains("-") || v.Contains("/"))) dateCount++;
                else if (TryParseLenientNumber(v, out double num))
                {
                    if (Math.Abs(num % 1) > double.Epsilon) decCount++;
                    else intCount++;
                }
                else textCount++;

                if (!string.IsNullOrEmpty(targetType) && mismatches.Count < mismatchSampleLimit)
                {
                    bool fails = false;
                    string reason = string.Empty;

                    if (targetType == "Entero" && (!TryParseLenientNumber(v, out double d) || Math.Abs(d % 1) > double.Epsilon))
                    {
                        fails = true; reason = "No es un número entero";
                    }
                    else if (targetType == "Decimal" && !TryParseLenientNumber(v, out _))
                    {
                        fails = true; reason = "No es un valor numérico";
                    }
                    else if (targetType == "Fecha" && !DateTime.TryParse(v, out _) &&
                             !(v.Length == 8 && Regex.IsMatch(v, @"^\d{8}$") && DateTime.TryParseExact(v, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)) &&
                             !(v.Length == 6 && Regex.IsMatch(v, @"^\d{6}$") && DateTime.TryParseExact(v, "yyyyMM", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)))
                    {
                        fails = true; reason = "Formato de fecha inválido";
                    }

                    if (fails) mismatches.Add(new MismatchRow { RowIndex = count + (hasHeaders ? 1 : 0), Value = v, Reason = reason });
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
            result.Recommendation = result.IsLikelyNumeric ? "Alto porcentaje numérico — considerar convertir la columna a Numérico."
                                  : (result.IntegerPercentage + result.DecimalPercentage >= 80.0 ? "Porcentaje numérico moderado — revisar los valores que no coinciden antes de convertir."
                                  : "Predomina Texto — no se recomienda conversión automática.");

            result.MismatchRows = mismatches;
            result.SampleValues = sampleValues;

            return result;
        }

        public List<DuplicateGroup> FindDuplicateGroups(string filePath, bool hasHeaders, string[] keyColumns, bool fullRow)
        {
            var result = new List<DuplicateGroup>();

            using var stream = File.OpenRead(filePath);
            string extension = Path.GetExtension(filePath).ToLower();
            bool isFlatFile = extension == ".csv" || extension == ".txt" || extension == ".dat";

            var configCsv = new ExcelReaderConfiguration() { AutodetectSeparators = new char[] { ',', ';', '\t', '|' } };
            using var reader = isFlatFile ? ExcelReaderFactory.CreateCsvReader(stream, configCsv) : ExcelReaderFactory.CreateReader(stream);

            int fieldCount = reader.FieldCount;
            if (fieldCount == 0) return result;

            List<int> compareIndexes = new List<int>();

            if (hasHeaders && reader.Read())
            {
                if (fullRow || keyColumns == null || keyColumns.Length == 0)
                {
                    compareIndexes = Enumerable.Range(0, fieldCount).ToList();
                }
                else
                {
                    for (int i = 0; i < fieldCount; i++)
                    {
                        var colName = reader.GetValue(i)?.ToString() ?? $"Columna_{i + 1}";
                        if (keyColumns.Contains(colName, StringComparer.OrdinalIgnoreCase)) compareIndexes.Add(i);
                    }
                }
            }
            else
            {
                if (fullRow || keyColumns == null || keyColumns.Length == 0) compareIndexes = Enumerable.Range(0, fieldCount).ToList();
                else
                {
                    for (int i = 0; i < fieldCount; i++)
                        if (keyColumns.Contains($"Columna_{i + 1}")) compareIndexes.Add(i);
                }
            }

            if (!compareIndexes.Any()) compareIndexes = Enumerable.Range(0, fieldCount).ToList();

            var seenOnce = new Dictionary<string, int>(StringComparer.Ordinal);
            var duplicates = new Dictionary<string, DuplicateGroup>(StringComparer.Ordinal);

            int rowIndex = hasHeaders ? 1 : 0;
            while (reader.Read())
            {
                rowIndex++;
                var parts = compareIndexes.Select(i => reader.GetValue(i)?.ToString() ?? string.Empty).ToArray();
                var key = string.Join("\u001F", parts);

                if (duplicates.TryGetValue(key, out var grp))
                {
                    grp.Count++;
                    grp.RowIndices.Add(rowIndex);
                }
                else if (seenOnce.TryGetValue(key, out int firstIndex))
                {
                    seenOnce.Remove(key);
                    duplicates[key] = new DuplicateGroup
                    {
                        Key = key,
                        Count = 2,
                        RowIndices = new List<int> { firstIndex, rowIndex },
                        SampleDisplay = string.Join(" | ", parts.Select(p => string.IsNullOrEmpty(p) ? "(vacío)" : p))
                    };
                }
                else
                {
                    seenOnce[key] = rowIndex;
                }
            }

            return duplicates.Values.OrderByDescending(g => g.Count).ToList();
        }
    }
}
