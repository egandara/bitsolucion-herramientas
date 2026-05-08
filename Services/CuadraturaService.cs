using ExcelDataReader;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Globalization;
using NotebookValidator.Web.Models;
using ClosedXML.Excel;

namespace NotebookValidator.Web.Services
{
    public interface ICuadraturaService
    {
        DataTable LeerExcel(string rutaArchivo, bool tieneEncabezados, string nombreHoja = null);
        DataTable AgruparDataTable(DataTable dt, List<string> llaves, List<string> columnasValores);
        List<SugerenciaMapeo> InferirColumnas(DataTable dt1, DataTable dt2);
        ResultadoCuadratura CompararDatos(DataTable dt1, DataTable dt2, List<string> llavesCol1, List<string> llavesCol2, List<string> columnasAComparar1, List<string> columnasAComparar2, List<double> tolerancias);
        byte[] GenerarExcelReporte(ResultadoCuadratura resultado);
    }

    public class CuadraturaService : ICuadraturaService
    {
        public DataTable LeerExcel(string rutaArchivo, bool tieneEncabezados, string nombreHoja = null)
        {
            using var stream = File.OpenRead(rutaArchivo);
            string extension = Path.GetExtension(rutaArchivo).ToLower();
            bool esArchivoPlano = extension == ".csv" || extension == ".txt" || extension == ".dat";
            IExcelDataReader reader;

            if (esArchivoPlano)
            {
                var configCsv = new ExcelReaderConfiguration() { AutodetectSeparators = new char[] { ',', ';', '\t', '|' } };
                reader = ExcelReaderFactory.CreateCsvReader(stream, configCsv);
            }
            else
            {
                reader = ExcelReaderFactory.CreateReader(stream);
            }

            var ds = reader.AsDataSet(new ExcelDataSetConfiguration() { ConfigureDataTable = (_) => new ExcelDataTableConfiguration() { UseHeaderRow = tieneEncabezados } });

            DataTable dt = new DataTable();
            if (ds.Tables.Count > 0)
            {
                // Si mandaron el nombre de la hoja y existe, usamos esa. Si no, usamos la primera por defecto.
                if (!string.IsNullOrEmpty(nombreHoja) && ds.Tables.Contains(nombreHoja))
                    dt = ds.Tables[nombreHoja];
                else
                    dt = ds.Tables[0];
            }

            if (!tieneEncabezados && dt.Columns.Count > 0)
                for (int i = 0; i < dt.Columns.Count; i++) dt.Columns[i].ColumnName = $"Columna_{i + 1}";

            return dt;
        }

        public DataTable AgruparDataTable(DataTable dt, List<string> llaves, List<string> columnasValores)
        {
            if (dt == null || dt.Rows.Count == 0) return dt;

            DataTable dtResumen = new DataTable();
            foreach (var col in llaves) dtResumen.Columns.Add(col, typeof(string));

            var columnasParaSumar = new List<string>();
            foreach (var colName in columnasValores)
            {
                if (dt.Columns.Contains(colName) && !dtResumen.Columns.Contains(colName))
                {
                    dtResumen.Columns.Add(colName, typeof(double));
                    columnasParaSumar.Add(colName);
                }
            }

            var agrupado = dt.AsEnumerable().GroupBy(row => string.Join("|", llaves.Select(k => row[k]?.ToString()?.Trim() ?? "")));

            foreach (var grupo in agrupado)
            {
                DataRow nuevaFila = dtResumen.NewRow();
                var primeraFila = grupo.First();

                foreach (var col in llaves) nuevaFila[col] = primeraFila[col]?.ToString() ?? "";

                foreach (var col in columnasParaSumar)
                {
                    double suma = 0;
                    foreach (var fila in grupo)
                    {
                        string valRaw = fila[col]?.ToString() ?? "0";
                        if (double.TryParse(valRaw.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double n))
                            suma += n;
                    }
                    nuevaFila[col] = suma;
                }
                dtResumen.Rows.Add(nuevaFila);
            }
            return dtResumen;
        }

        public List<SugerenciaMapeo> InferirColumnas(DataTable dt1, DataTable dt2)
        {
            var sugerencias = new List<SugerenciaMapeo>();
            var cols1 = dt1.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
            var cols2 = dt2.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();

            var exclusionKeywords = new List<string> { "seg_", "tramo", "clasificacion", "banco", "prestamo", "periodo", "matriz", "fecha", "estado", "tipo", "id", "sucursal", "rut", "dv" };

            foreach (var c1 in cols1)
            {
                string c1Lower = c1.ToLower();
                if (exclusionKeywords.Any(k => c1Lower.Contains(k))) continue;

                var exactMatch = cols2.FirstOrDefault(c => c.Equals(c1, StringComparison.OrdinalIgnoreCase));
                if (exactMatch != null)
                {
                    sugerencias.Add(new SugerenciaMapeo { ColumnaArchivo1 = c1, ColumnaArchivo2 = exactMatch, PorcentajeSimilitud = 100 });
                    continue;
                }

                string bestMatch = null;
                int maxSimilitud = 0;

                foreach (var c2 in cols2)
                {
                    string c2Lower = c2.ToLower();
                    if (exclusionKeywords.Any(k => c2Lower.Contains(k))) continue;

                    int distancia = CalcularLevenshtein(c1Lower, c2Lower);
                    int maxLen = Math.Max(c1.Length, c2.Length);
                    int similitud = maxLen == 0 ? 100 : (int)((1.0 - (double)distancia / maxLen) * 100);

                    if (similitud >= 85 && similitud > maxSimilitud)
                    {
                        maxSimilitud = similitud;
                        bestMatch = c2;
                    }
                }

                if (bestMatch != null)
                {
                    sugerencias.Add(new SugerenciaMapeo { ColumnaArchivo1 = c1, ColumnaArchivo2 = bestMatch, PorcentajeSimilitud = maxSimilitud });
                }
            }
            return sugerencias.OrderByDescending(s => s.PorcentajeSimilitud).ToList();
        }

        public ResultadoCuadratura CompararDatos(DataTable dt1, DataTable dt2, List<string> llavesCol1, List<string> llavesCol2, List<string> columnasAComparar1, List<string> columnasAComparar2, List<double> tolerancias)
        {
            var resultado = new ResultadoCuadratura();
            var dict2 = new Dictionary<string, DataRow>();

            foreach (DataRow row in dt2.Rows)
            {
                string key = string.Join("|", llavesCol2.Select(k => row[k]?.ToString()?.Trim() ?? ""));
                if (!dict2.ContainsKey(key)) dict2.Add(key, row);
            }

            var llavesProcesadasArchivo2 = new HashSet<string>();

            foreach (DataRow row1 in dt1.Rows)
            {
                string key1 = string.Join("|", llavesCol1.Select(k => row1[k]?.ToString()?.Trim() ?? ""));

                if (dict2.TryGetValue(key1, out DataRow row2))
                {
                    llavesProcesadasArchivo2.Add(key1);
                    bool hayDiferencia = false;
                    List<string> detallesFila = new List<string>();

                    for (int i = 0; i < columnasAComparar1.Count; i++)
                    {
                        string val1 = row1[columnasAComparar1[i]]?.ToString()?.Trim() ?? "";
                        string val2 = row2[columnasAComparar2[i]]?.ToString()?.Trim() ?? "";
                        double tol = (tolerancias.Count > i) ? tolerancias[i] : 0;

                        if (!SonValoresIguales(val1, val2, tol))
                        {
                            double diffNum = 0;
                            if (double.TryParse(val1.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double n1) &&
                                double.TryParse(val2.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double n2))
                            {
                                diffNum = n1 - n2;
                            }

                            resultado.RegistrosConDiferencias.Add(new DiferenciaRegistro
                            {
                                LlaveIdentificadora = key1,
                                ColumnaConFalla = columnasAComparar1[i],
                                ValorArchivo1 = val1,
                                ValorArchivo2 = val2,
                                Diferencia = (diffNum != 0) ? diffNum.ToString("N4") : "Distinto",
                                DiferenciaNumerica = diffNum
                            });
                            hayDiferencia = true;
                        }
                        else
                        {
                            detallesFila.Add($"{columnasAComparar1[i]}: {val1}");
                        }
                    }

                    if (!hayDiferencia)
                    {
                        resultado.TotalCoincidenciasExactas++;
                        resultado.RegistrosCuadrados.Add(new RegistroCuadrado
                        {
                            LlaveIdentificadora = key1,
                            DetalleValores = string.Join(" | ", detallesFila)
                        });
                    }
                }
                else
                {
                    resultado.HuerfanosArchivo1.Add(key1);
                }
            }

            foreach (var key2 in dict2.Keys)
            {
                if (!llavesProcesadasArchivo2.Contains(key2))
                {
                    resultado.HuerfanosArchivo2.Add(key2);
                }
            }

            return resultado;
        }

        public byte[] GenerarExcelReporte(ResultadoCuadratura resultado)
        {
            using var workbook = new XLWorkbook();

            var ws1 = workbook.Worksheets.Add("Diferencias");
            ws1.Cell(1, 1).Value = "Llave Primaria";
            ws1.Cell(1, 2).Value = "Columna";
            ws1.Cell(1, 3).Value = resultado.AliasArchivo1;
            ws1.Cell(1, 4).Value = resultado.AliasArchivo2;
            ws1.Cell(1, 5).Value = "Diferencia";

            for (int i = 0; i < resultado.RegistrosConDiferencias.Count; i++)
            {
                var reg = resultado.RegistrosConDiferencias[i];
                ws1.Cell(i + 2, 1).Value = reg.LlaveIdentificadora;
                ws1.Cell(i + 2, 2).Value = reg.ColumnaConFalla;
                ws1.Cell(i + 2, 3).Value = reg.ValorArchivo1;
                ws1.Cell(i + 2, 4).Value = reg.ValorArchivo2;
                ws1.Cell(i + 2, 5).Value = reg.Diferencia;
            }
            ws1.Columns().AdjustToContents();

            var ws2 = workbook.Worksheets.Add("Huérfanos");
            ws2.Cell(1, 1).Value = "Origen";
            ws2.Cell(1, 2).Value = "Llave Primaria";
            int rowH = 2;
            foreach (var h in resultado.HuerfanosArchivo1) { ws2.Cell(rowH, 1).Value = resultado.AliasArchivo1; ws2.Cell(rowH, 2).Value = h; rowH++; }
            foreach (var h in resultado.HuerfanosArchivo2) { ws2.Cell(rowH, 1).Value = resultado.AliasArchivo2; ws2.Cell(rowH, 2).Value = h; rowH++; }
            ws2.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }

        private bool SonValoresIguales(string val1, string val2, double tolerancia)
        {
            if (val1 == val2) return true;
            if (string.IsNullOrEmpty(val1) || string.IsNullOrEmpty(val2)) return false;

            string numParse1 = val1.Replace(",", ".");
            string numParse2 = val2.Replace(",", ".");

            if (double.TryParse(numParse1, NumberStyles.Any, CultureInfo.InvariantCulture, out double n1) &&
                double.TryParse(numParse2, NumberStyles.Any, CultureInfo.InvariantCulture, out double n2))
            {
                return Math.Abs(n1 - n2) <= tolerancia;
            }

            if (DateTime.TryParse(val1, out DateTime fecha1) && DateTime.TryParse(val2, out DateTime fecha2))
                return fecha1.Date == fecha2.Date;

            return false;
        }

        private int CalcularLevenshtein(string s, string t)
        {
            if (string.IsNullOrEmpty(s)) return string.IsNullOrEmpty(t) ? 0 : t.Length;
            if (string.IsNullOrEmpty(t)) return s.Length;
            int[] v0 = new int[t.Length + 1]; int[] v1 = new int[t.Length + 1];
            for (int i = 0; i < v0.Length; i++) v0[i] = i;
            for (int i = 0; i < s.Length; i++)
            {
                v1[0] = i + 1;
                for (int j = 0; j < t.Length; j++)
                {
                    int cost = (s[i] == t[j]) ? 0 : 1;
                    v1[j + 1] = Math.Min(v1[j] + 1, Math.Min(v0[j + 1] + 1, v0[j] + cost));
                }
                for (int j = 0; j < v0.Length; j++) v0[j] = v1[j];
            }
            return v1[t.Length];
        }
    }
}
