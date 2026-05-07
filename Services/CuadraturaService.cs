using ExcelDataReader;
using Microsoft.AspNetCore.Http;
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
        DataTable LeerExcel(string rutaArchivo, bool tieneEncabezados);
        DataTable AgruparDataTable(DataTable dt, List<string> llaves, List<string> columnasValores);
        List<SugerenciaMapeo> InferirColumnas(DataTable dt1, DataTable dt2);
        ResultadoCuadratura CompararDatos(DataTable dt1, DataTable dt2, List<string> llavesCol1, List<string> llavesCol2, List<string> columnasAComparar1, List<string> columnasAComparar2, List<double> tolerancias);
        byte[] GenerarExcelReporte(ResultadoCuadratura resultado);
    }

    public class CuadraturaService : ICuadraturaService
    {
        public DataTable LeerExcel(string rutaArchivo, bool tieneEncabezados)
        {
            using var stream = File.OpenRead(rutaArchivo);
            string extension = Path.GetExtension(rutaArchivo).ToLower();
            bool esArchivoPlano = extension == ".csv" || extension == ".txt" || extension == ".dat";
            IExcelDataReader reader;

            if (esArchivoPlano)
            {
                var configuracionCsv = new ExcelReaderConfiguration() { AutodetectSeparators = new char[] { ',', ';', '\t', '|', '~', '^' } };
                reader = ExcelReaderFactory.CreateCsvReader(stream, configuracionCsv);
            }
            else reader = ExcelReaderFactory.CreateReader(stream);

            var result = reader.AsDataSet(new ExcelDataSetConfiguration() { ConfigureDataTable = (_) => new ExcelDataTableConfiguration() { UseHeaderRow = tieneEncabezados } });
            var dt = result.Tables.Count > 0 ? result.Tables[0] : new DataTable();

            if (!tieneEncabezados && dt.Columns.Count > 0)
                for (int i = 0; i < dt.Columns.Count; i++) dt.Columns[i].ColumnName = $"columna_{i + 1}";

            return dt;
        }

        public DataTable AgruparDataTable(DataTable dt, List<string> llaves, List<string> columnasValores)
        {
            DataTable dtResumen = new DataTable();
            foreach (var col in llaves) dtResumen.Columns.Add(col, typeof(string));
            foreach (var col in columnasValores) dtResumen.Columns.Add(col, typeof(double));

            var query = dt.AsEnumerable()
                .GroupBy(row => string.Join("|", llaves.Select(k => row[k]?.ToString()?.Trim() ?? "")))
                .Select(g => {
                    DataRow newRow = dtResumen.NewRow();
                    var primeraFila = g.First();
                    foreach (var col in llaves) newRow[col] = primeraFila[col];
                    foreach (var col in columnasValores)
                    {
                        newRow[col] = g.Sum(r => {
                            double.TryParse(r[col]?.ToString().Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double val);
                            return val;
                        });
                    }
                    return newRow;
                });

            foreach (var r in query) dtResumen.Rows.Add(r);
            return dtResumen;
        }

        public List<SugerenciaMapeo> InferirColumnas(DataTable dt1, DataTable dt2)
        {
            var sugerencias = new List<SugerenciaMapeo>();
            if (dt1 == null || dt2 == null || dt1.Columns.Count == 0 || dt2.Columns.Count == 0) return sugerencias;
            var columnasDt2 = dt2.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();

            foreach (DataColumn col1 in dt1.Columns)
            {
                if (!EsColumnaNumerica(dt1, col1)) continue;

                string mejorCoincidencia = string.Empty;
                int menorDistancia = int.MaxValue;
                foreach (var col2Name in columnasDt2)
                {
                    int distancia = CalcularLevenshtein(col1.ColumnName.ToLower(), col2Name.ToLower());
                    if (distancia < menorDistancia) { menorDistancia = distancia; mejorCoincidencia = col2Name; }
                }
                int longitudMax = Math.Max(col1.ColumnName.Length, mejorCoincidencia.Length);
                int similitud = (int)((1.0 - ((double)menorDistancia / longitudMax)) * 100);

                if (similitud > 60)
                {
                    DataColumn col2Obj = dt2.Columns[mejorCoincidencia];
                    if (EsColumnaNumerica(dt2, col2Obj))
                    {
                        sugerencias.Add(new SugerenciaMapeo { ColumnaArchivo1 = col1.ColumnName, ColumnaArchivo2 = mejorCoincidencia, PorcentajeSimilitud = similitud });
                    }
                }
            }
            return sugerencias;
        }

        private bool EsColumnaNumerica(DataTable dt, DataColumn col)
        {
            var type = col.DataType;
            if (type == typeof(int) || type == typeof(double) || type == typeof(decimal) || type == typeof(float) || type == typeof(long) || type == typeof(short))
                return true;

            int numProbados = 0;
            int numNumericos = 0;

            foreach (DataRow row in dt.Rows)
            {
                var val = row[col]?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(val))
                {
                    numProbados++;
                    string numParse = val.Replace(",", ".");
                    if (double.TryParse(numParse, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                    {
                        numNumericos++;
                    }
                    else
                    {
                        return false;
                    }
                }
                if (numProbados >= 5) break;
            }

            return numProbados > 0 && numProbados == numNumericos;
        }

        public ResultadoCuadratura CompararDatos(DataTable dt1, DataTable dt2, List<string> llavesCol1, List<string> llavesCol2, List<string> columnasAComparar1, List<string> columnasAComparar2, List<double> tolerancias)
        {
            var resultado = new ResultadoCuadratura();
            var diccionarioArchivo2 = new Dictionary<string, DataRow>();
            if (dt1 == null || dt2 == null) return resultado;

            foreach (DataRow row in dt2.Rows)
            {
                var llave = ConstruirLlaveCompuesta(row, llavesCol2);
                if (!string.IsNullOrEmpty(llave) && !diccionarioArchivo2.ContainsKey(llave)) diccionarioArchivo2.Add(llave, row);
            }

            foreach (DataRow fila1 in dt1.Rows)
            {
                var llave1 = ConstruirLlaveCompuesta(fila1, llavesCol1);
                if (string.IsNullOrEmpty(llave1)) continue;

                if (diccionarioArchivo2.TryGetValue(llave1, out DataRow fila2))
                {
                    bool esMatchExacto = true;
                    for (int i = 0; i < columnasAComparar1.Count; i++)
                    {
                        var val1Raw = fila1[columnasAComparar1[i]]?.ToString()?.Trim() ?? "";
                        var val2Raw = fila2[columnasAComparar2[i]]?.ToString()?.Trim() ?? "";
                        double toleranciaActual = tolerancias[i];

                        if (!SonValoresEquivalentes(val1Raw, val2Raw, toleranciaActual))
                        {
                            esMatchExacto = false;
                            string difTexto = "N/A";
                            double? difNumerica = null; // NUEVO: Extraemos el valor numérico

                            if (double.TryParse(val1Raw.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double n1) &&
                                double.TryParse(val2Raw.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double n2))
                            {
                                difNumerica = n1 - n2;
                                difTexto = difNumerica.Value.ToString("N4", CultureInfo.CurrentCulture);
                            }

                            resultado.RegistrosConDiferencias.Add(new DiferenciaRegistro
                            {
                                LlaveIdentificadora = llave1.Replace("||", " - "),
                                ColumnaConFalla = columnasAComparar1[i],
                                ValorArchivo1 = val1Raw,
                                ValorArchivo2 = val2Raw,
                                Diferencia = difTexto,
                                DiferenciaNumerica = difNumerica // NUEVO: Guardamos para stats
                            });
                        }
                    }
                    if (esMatchExacto)
                    {
                        resultado.TotalCoincidenciasExactas++;
                        resultado.RegistrosCuadrados.Add(new RegistroCuadrado { LlaveIdentificadora = llave1.Replace("||", " - "), DetalleValores = "Match Perfecto" });
                    }
                    diccionarioArchivo2.Remove(llave1);
                }
                else resultado.HuerfanosArchivo1.Add(llave1.Replace("||", " - "));
            }
            resultado.HuerfanosArchivo2 = diccionarioArchivo2.Keys.Select(k => k.Replace("||", " - ")).ToList();
            return resultado;
        }

        public byte[] GenerarExcelReporte(ResultadoCuadratura resultado)
        {
            using (var workbook = new XLWorkbook())
            {
                var wsDif = workbook.Worksheets.Add("Diferencias");
                wsDif.Cell(1, 1).Value = "Llave Identificadora";
                wsDif.Cell(1, 2).Value = "Columna";
                wsDif.Cell(1, 3).Value = $"Valor {resultado.AliasArchivo1}";
                wsDif.Cell(1, 4).Value = $"Valor {resultado.AliasArchivo2}";
                wsDif.Cell(1, 5).Value = "Diferencia";

                wsDif.Range("A1:E1").Style.Font.Bold = true;
                wsDif.Range("A1:E1").Style.Fill.BackgroundColor = XLColor.Yellow;

                for (int i = 0; i < resultado.RegistrosConDiferencias.Count; i++)
                {
                    var item = resultado.RegistrosConDiferencias[i];
                    wsDif.Cell(i + 2, 1).Value = item.LlaveIdentificadora;
                    wsDif.Cell(i + 2, 2).Value = item.ColumnaConFalla;
                    wsDif.Cell(i + 2, 3).Value = item.ValorArchivo1;
                    wsDif.Cell(i + 2, 4).Value = item.ValorArchivo2;
                    wsDif.Cell(i + 2, 5).Value = item.Diferencia;
                }
                wsDif.Columns().AdjustToContents();

                var wsH = workbook.Worksheets.Add("Huérfanos");
                wsH.Cell(1, 1).Value = "Origen"; wsH.Cell(1, 2).Value = "Llave";
                wsH.Range("A1:B1").Style.Font.Bold = true; wsH.Range("A1:B1").Style.Fill.BackgroundColor = XLColor.LightCoral;

                int row = 2;
                foreach (var h in resultado.HuerfanosArchivo1) { wsH.Cell(row, 1).Value = $"Sólo en {resultado.AliasArchivo1}"; wsH.Cell(row++, 2).Value = h; }
                foreach (var h in resultado.HuerfanosArchivo2) { wsH.Cell(row, 1).Value = $"Sólo en {resultado.AliasArchivo2}"; wsH.Cell(row++, 2).Value = h; }
                wsH.Columns().AdjustToContents();

                var wsR = workbook.Worksheets.Add("Resumen");
                wsR.Cell(1, 1).Value = "Métrica"; wsR.Cell(1, 2).Value = "Valor";
                wsR.Range("A1:B1").Style.Font.Bold = true; wsR.Range("A1:B1").Style.Fill.BackgroundColor = XLColor.LightGray;
                wsR.Cell(2, 1).Value = "Coincidencias Exactas"; wsR.Cell(2, 2).Value = resultado.TotalCoincidenciasExactas;
                wsR.Cell(3, 1).Value = "Registros con Diferencias (Únicos)"; wsR.Cell(3, 2).Value = resultado.RegistrosConDiferencias.Select(d => d.LlaveIdentificadora).Distinct().Count();
                wsR.Cell(4, 1).Value = "Huérfanos Totales"; wsR.Cell(4, 2).Value = resultado.HuerfanosArchivo1.Count + resultado.HuerfanosArchivo2.Count;

                // NUEVO: ESTADÍSTICAS POR CAMPO EN EXCEL
                int rRow = 6;
                wsR.Cell(rRow, 1).Value = "--- IMPACTO ESTADÍSTICO POR CAMPO ---";
                wsR.Range(rRow, 1, rRow, 4).Merge().Style.Font.Bold = true;
                wsR.Range(rRow, 1, rRow, 4).Style.Fill.BackgroundColor = XLColor.LightBlue;
                rRow++;
                wsR.Cell(rRow, 1).Value = "Campo";
                wsR.Cell(rRow, 2).Value = "Cantidad Errores";
                wsR.Cell(rRow, 3).Value = "Suma Neta (Con signos)";
                wsR.Cell(rRow, 4).Value = "Riesgo Absoluto (Solo positivos)";
                wsR.Range(rRow, 1, rRow, 4).Style.Font.Bold = true;
                rRow++;

                var gruposEstadistica = resultado.RegistrosConDiferencias.GroupBy(d => d.ColumnaConFalla);
                foreach (var g in gruposEstadistica)
                {
                    wsR.Cell(rRow, 1).Value = g.Key;
                    wsR.Cell(rRow, 2).Value = g.Count();
                    wsR.Cell(rRow, 3).Value = g.Sum(x => x.DiferenciaNumerica ?? 0).ToString("N4");
                    wsR.Cell(rRow, 4).Value = g.Sum(x => Math.Abs(x.DiferenciaNumerica ?? 0)).ToString("N4");
                    rRow++;
                }

                wsR.Columns().AdjustToContents();

                using (var stream = new MemoryStream()) { workbook.SaveAs(stream); return stream.ToArray(); }
            }
        }

        private string ConstruirLlaveCompuesta(DataRow row, List<string> columnasLlave)
        {
            var valores = new List<string>();
            foreach (var col in columnasLlave) valores.Add(row[col]?.ToString()?.Trim() ?? "");
            return string.Join("||", valores);
        }

        private bool SonValoresEquivalentes(string val1, string val2, double tolerancia)
        {
            if (string.Equals(val1, val2, StringComparison.OrdinalIgnoreCase)) return true;
            if (string.IsNullOrEmpty(val1) || string.IsNullOrEmpty(val2)) return false;
            string numParse1 = val1.Replace(",", ".");
            string numParse2 = val2.Replace(",", ".");

            if (double.TryParse(numParse1, NumberStyles.Any, CultureInfo.InvariantCulture, out double n1) &&
                double.TryParse(numParse2, NumberStyles.Any, CultureInfo.InvariantCulture, out double n2))
            {
                return Math.Abs(n1 - n2) <= tolerancia;
            }

            if (DateTime.TryParse(val1, out DateTime fecha1) && DateTime.TryParse(val2, out DateTime fecha2)) return fecha1.Date == fecha2.Date;
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
                    v1[j + 1] = Math.Min(Math.Min(v1[j] + 1, v0[j + 1] + 1), v0[j] + cost);
                }
                for (int j = 0; j < v0.Length; j++) v0[j] = v1[j];
            }
            return v1[t.Length];
        }
    }
}
