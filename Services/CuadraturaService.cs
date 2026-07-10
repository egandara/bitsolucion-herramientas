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
        public DataTable LeerExcel(string rutaArchivo, bool tieneEncabezados, string nombreHoja = null);
        public DataTable AgruparDataTable(DataTable dt, List<string> llaves, List<string> columnasValores);
        public List<SugerenciaMapeo> InferirColumnas(DataTable dt1, DataTable dt2);
        public ResultadoCuadratura CompararDatos(DataTable dt1, DataTable dt2, List<string> llavesCol1, List<string> llavesCol2, List<string> columnasAComparar1, List<string> columnasAComparar2, List<double> tolerancias);
        public byte[] GenerarExcelReporte(ResultadoCuadratura resultado);

        public ResultadoEstructura ValidarEstructuras(string ruta1, string ruta2, string alias1, string alias2, bool tieneEncabezados1, bool tieneEncabezados2, string hoja1 = null, string hoja2 = null);
        public byte[] GenerarExcelReporteEstructura(ResultadoEstructura resultado);
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

        // ==========================================
        // MOTOR DE VALIDACIÓN ESTRUCTURAL Y METADATOS
        // ==========================================
        public ResultadoEstructura ValidarEstructuras(string ruta1, string ruta2, string alias1, string alias2, bool tieneEncabezados1, bool tieneEncabezados2, string hoja1 = null, string hoja2 = null)
        {
            var resultado = new ResultadoEstructura();

            DataTable dt1 = LeerExcel(ruta1, tieneEncabezados1, hoja1);
            resultado.Archivo1 = AnalizarEsquema(dt1, alias1, ruta1);
            resultado.MuestraArchivo1 = ExtraerMuestraAleatoria(dt1, 10);

            DataTable dt2 = LeerExcel(ruta2, tieneEncabezados2, hoja2);
            resultado.Archivo2 = AnalizarEsquema(dt2, alias2, ruta2);
            resultado.MuestraArchivo2 = ExtraerMuestraAleatoria(dt2, 10);

            if (resultado.Archivo1.SaltoLinea != resultado.Archivo2.SaltoLinea)
                resultado.AdvertenciasFisicas.Add($"Diferente salto de línea detectado: {resultado.Archivo1.Alias} usa [{resultado.Archivo1.SaltoLinea}] mientras que {resultado.Archivo2.Alias} usa [{resultado.Archivo2.SaltoLinea}].");

            if (resultado.Archivo1.Separador != resultado.Archivo2.Separador && resultado.Archivo1.Separador != "N/A (Excel)" && resultado.Archivo2.Separador != "N/A (Excel)")
                resultado.AdvertenciasFisicas.Add($"Diferente delimitador de columnas: {resultado.Archivo1.Alias} usa [{resultado.Archivo1.Separador}] y {resultado.Archivo2.Alias} usa [{resultado.Archivo2.Separador}].");

            if (resultado.Archivo1.Encoding != resultado.Archivo2.Encoding)
                resultado.AdvertenciasFisicas.Add($"Diferente codificación (Encoding): {resultado.Archivo1.Alias} está en [{resultado.Archivo1.Encoding}] y {resultado.Archivo2.Alias} en [{resultado.Archivo2.Encoding}].");

            var todasLasColumnas = resultado.Archivo1.Columnas.Select(c => c.Nombre)
                .Union(resultado.Archivo2.Columnas.Select(c => c.Nombre))
                .Distinct()
                .ToList();

            foreach (var colName in todasLasColumnas)
            {
                var col1 = resultado.Archivo1.Columnas.FirstOrDefault(c => c.Nombre.Equals(colName, StringComparison.OrdinalIgnoreCase));
                var col2 = resultado.Archivo2.Columnas.FirstOrDefault(c => c.Nombre.Equals(colName, StringComparison.OrdinalIgnoreCase));

                var comp = new ComparacionColumna
                {
                    NombreColumna = col1?.Nombre ?? col2?.Nombre,
                    TipoArchivo1 = col1?.TipoInferido ?? "N/A",
                    TipoArchivo2 = col2?.TipoInferido ?? "N/A",
                    MetadatosColumnaA = col1,
                    MetadatosColumnaB = col2
                };

                if (col1 == null) { comp.Estado = "Faltante en A"; resultado.ColumnasFaltantes++; }
                else if (col2 == null) { comp.Estado = "Faltante en B"; resultado.ColumnasFaltantes++; }
                else if (col1.TipoInferido != col2.TipoInferido && col1.TipoInferido != "Nulo/Vacío" && col2.TipoInferido != "Nulo/Vacío")
                { comp.Estado = "Conflicto de Tipo"; resultado.TiposDiferentes++; }
                else { comp.Estado = "Match"; }

                resultado.ComparacionColumnas.Add(comp);
            }

            resultado.EstructurasCoinciden = (resultado.ColumnasFaltantes == 0 && resultado.TiposDiferentes == 0);
            return resultado;
        }

        private MetadatosArchivo AnalizarEsquema(DataTable dt, string alias, string rutaArchivo)
        {
            var meta = new MetadatosArchivo
            {
                Alias = string.IsNullOrWhiteSpace(alias) ? "Archivo" : alias,
                Extension = Path.GetExtension(rutaArchivo).ToUpper(),
                TotalColumnas = dt.Columns.Count,
                TotalFilasMuestra = Math.Min(dt.Rows.Count, 150)
            };

            ExtraerMetadatosFisicos(rutaArchivo, meta);

            foreach (DataColumn col in dt.Columns)
            {
                var info = new ColumnaInfo
                {
                    Nombre = col.ColumnName,
                    LongitudMaxima = 0,
                    LongitudMinima = int.MaxValue,
                    NulosDetectados = 0
                };
                var tiposDetectados = new Dictionary<string, int>();

                for (int i = 0; i < meta.TotalFilasMuestra; i++)
                {
                    string valor = dt.Rows[i][col]?.ToString()?.Trim() ?? "";
                    if (string.IsNullOrEmpty(valor)) { info.NulosDetectados++; continue; }

                    int len = valor.Length;
                    if (len > info.LongitudMaxima) info.LongitudMaxima = len;
                    if (len < info.LongitudMinima) info.LongitudMinima = len;
                    if (len > 1 && valor.StartsWith("0") && valor.All(char.IsDigit)) info.TieneCerosALaIzquierda = true;

                    string tipo = InferirTipoDato(valor);
                    if (tiposDetectados.ContainsKey(tipo)) tiposDetectados[tipo]++;
                    else tiposDetectados[tipo] = 1;
                }

                if (info.LongitudMinima == int.MaxValue) info.LongitudMinima = 0;
                info.PorcentajeNulos = meta.TotalFilasMuestra > 0 ? Math.Round((double)info.NulosDetectados / meta.TotalFilasMuestra * 100, 2) : 100;

                if (tiposDetectados.Count == 0) info.TipoInferido = "Nulo/Vacío";
                else
                {
                    if (tiposDetectados.ContainsKey("Texto")) info.TipoInferido = "Texto";
                    else if (tiposDetectados.ContainsKey("Decimal")) info.TipoInferido = "Decimal";
                    else info.TipoInferido = tiposDetectados.OrderByDescending(x => x.Value).First().Key;
                }

                meta.Columnas.Add(info);
            }
            return meta;
        }

        private void ExtraerMetadatosFisicos(string ruta, MetadatosArchivo meta)
        {
            var fi = new FileInfo(ruta);
            meta.PesoArchivo = FormatearBytes(fi.Length);
            bool isFlat = meta.Extension == ".CSV" || meta.Extension == ".TXT" || meta.Extension == ".DAT";

            try
            {
                using var fs = new FileStream(ruta, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(fs, System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 4096);
                char[] buffer = new char[4096];
                int charsRead = reader.Read(buffer, 0, buffer.Length);
                string chunk = new string(buffer, 0, charsRead);

                meta.Encoding = reader.CurrentEncoding.EncodingName;

                if (chunk.Contains("\r\n")) meta.SaltoLinea = "CRLF (Windows)";
                else if (chunk.Contains("\n")) meta.SaltoLinea = "LF (Unix/Linux)";
                else if (chunk.Contains("\r")) meta.SaltoLinea = "CR (Mac OS Clásico)";
                else meta.SaltoLinea = "No detectado";

                if (isFlat)
                {
                    string primeraLinea = chunk.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
                    var separadores = new Dictionary<string, int>
                    {
                        { "Coma (,)", primeraLinea.Count(c => c == ',') }, { "Punto y coma (;)", primeraLinea.Count(c => c == ';') },
                        { "Tabulador (\\t)", primeraLinea.Count(c => c == '\t') }, { "Pipe (|)", primeraLinea.Count(c => c == '|') }
                    };
                    var mejor = separadores.OrderByDescending(x => x.Value).First();
                    meta.Separador = mejor.Value > 0 ? mejor.Key : "No detectado";
                }
                else { meta.Separador = "N/A (Excel)"; }
            }
            catch { meta.Encoding = "Desconocido"; meta.SaltoLinea = "Desconocido"; meta.Separador = "Desconocido"; }
        }

        private string FormatearBytes(long bytes)
        {
            string[] suf = { "B", "KB", "MB", "GB" };
            if (bytes == 0) return "0 B";
            long i = (long)Math.Floor(Math.Log(bytes, 1024));
            return Math.Round(bytes / Math.Pow(1024, i), 2) + " " + suf[i];
        }

        private MuestraDatos ExtraerMuestraAleatoria(DataTable dt, int cantidad)
        {
            var muestra = new MuestraDatos();
            foreach (DataColumn col in dt.Columns) muestra.Encabezados.Add(col.ColumnName);

            var rnd = new Random();
            var indices = Enumerable.Range(0, dt.Rows.Count).OrderBy(x => rnd.Next()).Take(cantidad).ToList();

            foreach (var idx in indices)
            {
                var filaStr = new List<string>();
                foreach (DataColumn col in dt.Columns) filaStr.Add(dt.Rows[idx][col]?.ToString() ?? "");
                muestra.Filas.Add(filaStr);
            }
            return muestra;
        }

        private string InferirTipoDato(string valor)
        {
            if (bool.TryParse(valor, out _)) return "Booleano";
            if (int.TryParse(valor, out _)) return "Entero";

            string parseDec = valor.Replace(",", ".");
            if (double.TryParse(parseDec, NumberStyles.Any, CultureInfo.InvariantCulture, out _)) return "Decimal";

            string[] formatosFecha = { "yyyy-MM-dd", "dd/MM/yyyy", "MM/dd/yyyy", "yyyy/MM/dd", "dd-MM-yyyy", "yyyyMMdd", "dd-MM-yyyy HH:mm:ss" };
            if (DateTime.TryParseExact(valor, formatosFecha, CultureInfo.InvariantCulture, DateTimeStyles.None, out _)) return "Fecha";
            if (DateTime.TryParse(valor, out _)) return "Fecha";

            return "Texto";
        }

        public byte[] GenerarExcelReporteEstructura(ResultadoEstructura resultado)
        {
            using var workbook = new XLWorkbook();

            // ── HOJA 1: REPORTE DE ESTRUCTURAS ──
            var ws1 = workbook.Worksheets.Add("Mapeo de Estructuras");
            ws1.ShowGridLines = false; // Sin cuadrícula general

            // Título Principal
            ws1.Cell("A1").Value = "REPORTE DE VALIDACIÓN ESTRUCTURAL";
            var titleRange = ws1.Range("A1:F2");
            titleRange.Merge().Style.Font.SetFontSize(16).Font.SetBold().Fill.SetBackgroundColor(XLColor.FromArgb(11, 13, 22)).Font.SetFontColor(XLColor.White).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center).Alignment.SetVertical(XLAlignmentVerticalValues.Center);

            // Resumen de Metadatos (Filas separadas)
            ws1.Cell("B4").Value = "METADATOS";
            ws1.Cell("C4").Value = resultado.Archivo1.Alias;
            ws1.Cell("D4").Value = resultado.Archivo2.Alias;
            ws1.Range("B4:D4").Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromArgb(13, 202, 240)).Font.SetFontColor(XLColor.Black).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws1.Cell("B5").Value = "Nombre del Archivo"; ws1.Cell("C5").Value = resultado.Archivo1.NombreArchivo; ws1.Cell("D5").Value = resultado.Archivo2.NombreArchivo;
            ws1.Cell("B6").Value = "Codificación"; ws1.Cell("C6").Value = resultado.Archivo1.Encoding; ws1.Cell("D6").Value = resultado.Archivo2.Encoding;
            ws1.Cell("B7").Value = "Separador"; ws1.Cell("C7").Value = resultado.Archivo1.Separador; ws1.Cell("D7").Value = resultado.Archivo2.Separador;
            ws1.Cell("B8").Value = "Salto de Línea"; ws1.Cell("C8").Value = resultado.Archivo1.SaltoLinea; ws1.Cell("D8").Value = resultado.Archivo2.SaltoLinea;
            ws1.Cell("B9").Value = "Total Columnas"; ws1.Cell("C9").Value = resultado.Archivo1.TotalColumnas; ws1.Cell("D9").Value = resultado.Archivo2.TotalColumnas;

            ws1.Range("B5:B9").Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.LightGray).Font.SetFontColor(XLColor.Black);
            ws1.Range("B4:D9").Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin);

            // Evaluación de coincidencia en Metadatos (Verde suave = coinciden, Rojo suave = diferencia)
            for (int r = 6; r <= 9; r++)
            {
                if (ws1.Cell(r, 3).Value.ToString() == ws1.Cell(r, 4).Value.ToString())
                {
                    ws1.Range(r, 3, r, 4).Style.Fill.SetBackgroundColor(XLColor.FromArgb(226, 239, 218)); // Verde Pastel
                    ws1.Range(r, 3, r, 4).Style.Font.SetFontColor(XLColor.FromArgb(55, 86, 35));
                }
                else
                {
                    ws1.Range(r, 3, r, 4).Style.Fill.SetBackgroundColor(XLColor.FromArgb(252, 228, 214)); // Naranja Pastel
                    ws1.Range(r, 3, r, 4).Style.Font.SetFontColor(XLColor.FromArgb(192, 0, 0)).Font.SetBold();
                }
            }

            // Tabla de Mapeo
            int startRow = 11;
            ws1.Cell(startRow, 1).Value = "Columna (Atributo)";
            ws1.Range(startRow, 1, startRow + 1, 1).Merge();

            ws1.Cell(startRow, 2).Value = $"Análisis: {resultado.Archivo1.Alias}";
            ws1.Range(startRow, 2, startRow, 3).Merge();
            ws1.Cell(startRow + 1, 2).Value = "Tipo Inferido";
            ws1.Cell(startRow + 1, 3).Value = "Formato y Nulos";

            ws1.Cell(startRow, 4).Value = $"Análisis: {resultado.Archivo2.Alias}";
            ws1.Range(startRow, 4, startRow, 5).Merge();
            ws1.Cell(startRow + 1, 4).Value = "Tipo Inferido";
            ws1.Cell(startRow + 1, 5).Value = "Formato y Nulos";

            ws1.Cell(startRow, 6).Value = "Estado de Validación";
            ws1.Range(startRow, 6, startRow + 1, 6).Merge();

            var tableHeader = ws1.Range(startRow, 1, startRow + 1, 6);
            tableHeader.Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromArgb(11, 13, 22)).Font.SetFontColor(XLColor.White).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center).Alignment.SetVertical(XLAlignmentVerticalValues.Center);
            tableHeader.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin);

            // Llenado de Datos (con colores sutiles diferenciadores)
            int row = startRow + 2;
            foreach (var comp in resultado.ComparacionColumnas)
            {
                ws1.Cell(row, 1).Value = comp.NombreColumna;
                ws1.Cell(row, 1).Style.Fill.SetBackgroundColor(XLColor.White);

                // Colores para Archivo 1 (Azul muy claro)
                ws1.Range(row, 2, row, 3).Style.Fill.SetBackgroundColor(XLColor.AliceBlue);
                ws1.Cell(row, 2).Value = comp.TipoArchivo1;
                if (comp.MetadatosColumnaA != null)
                {
                    var ma = comp.MetadatosColumnaA;
                    string lenA = ma.LongitudMinima == ma.LongitudMaxima ? $"Fijo: {ma.LongitudMaxima}" : $"Var: {ma.LongitudMinima}-{ma.LongitudMaxima}";
                    string zA = ma.TieneCerosALaIzquierda ? " | Zero-Padded" : "";
                    ws1.Cell(row, 3).Value = $"{lenA} | Nulls: {ma.PorcentajeNulos}%{zA}";
                }

                // Colores para Archivo 2 (Crema / Amarillo muy claro)
                ws1.Range(row, 4, row, 5).Style.Fill.SetBackgroundColor(XLColor.FloralWhite);
                ws1.Cell(row, 4).Value = comp.TipoArchivo2;
                if (comp.MetadatosColumnaB != null)
                {
                    var mb = comp.MetadatosColumnaB;
                    string lenB = mb.LongitudMinima == mb.LongitudMaxima ? $"Fijo: {mb.LongitudMaxima}" : $"Var: {mb.LongitudMinima}-{mb.LongitudMaxima}";
                    string zB = mb.TieneCerosALaIzquierda ? " | Zero-Padded" : "";
                    ws1.Cell(row, 5).Value = $"{lenB} | Nulls: {mb.PorcentajeNulos}%{zB}";
                }

                ws1.Cell(row, 6).Value = comp.Estado;

                // Formato Condicional para Estado
                if (comp.Estado == "Match")
                {
                    ws1.Cell(row, 6).Style.Font.SetFontColor(XLColor.ForestGreen).Font.SetBold().Fill.SetBackgroundColor(XLColor.LightGreen);
                }
                else
                {
                    ws1.Cell(row, 6).Style.Font.SetFontColor(XLColor.DarkRed).Font.SetBold().Fill.SetBackgroundColor(XLColor.LightPink);
                }

                row++;
            }

            var dataRange = ws1.Range(startRow + 2, 1, row - 1, 6);
            dataRange.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin);
            ws1.Range(startRow + 1, 1, row - 1, 6).SetAutoFilter();
            ws1.Columns().AdjustToContents();

            // ── HOJA 2: MUESTRAS DE DATOS EN UNA SOLA HOJA ──
            var wsM = workbook.Worksheets.Add("Muestras de Datos");
            wsM.ShowGridLines = false; // Sin cuadrícula general
            int rM = 1;

            // Bloque Muestra 1
            if (resultado.MuestraArchivo1.Encabezados.Any())
            {
                wsM.Cell(rM, 1).Value = $"MUESTRA ALEATORIA: {resultado.Archivo1.Alias} ({resultado.Archivo1.NombreArchivo})";
                wsM.Range(rM, 1, rM, resultado.MuestraArchivo1.Encabezados.Count).Merge().Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromArgb(13, 202, 240)).Font.SetFontColor(XLColor.Black);
                rM++;

                for (int c = 0; c < resultado.MuestraArchivo1.Encabezados.Count; c++)
                {
                    wsM.Cell(rM, c + 1).Value = resultado.MuestraArchivo1.Encabezados[c];
                    wsM.Cell(rM, c + 1).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.LightGray);
                }
                rM++;

                for (int i = 0; i < resultado.MuestraArchivo1.Filas.Count; i++)
                {
                    for (int c = 0; c < resultado.MuestraArchivo1.Filas[i].Count; c++)
                    {
                        wsM.Cell(rM, c + 1).Value = resultado.MuestraArchivo1.Filas[i][c];
                        wsM.Cell(rM, c + 1).Style.Fill.SetBackgroundColor(XLColor.White);
                    }
                    rM++;
                }
                rM += 2; // Espacio entre muestras
            }

            // Bloque Muestra 2
            if (resultado.MuestraArchivo2.Encabezados.Any())
            {
                wsM.Cell(rM, 1).Value = $"MUESTRA ALEATORIA: {resultado.Archivo2.Alias} ({resultado.Archivo2.NombreArchivo})";
                wsM.Range(rM, 1, rM, resultado.MuestraArchivo2.Encabezados.Count).Merge().Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromArgb(255, 193, 7)).Font.SetFontColor(XLColor.Black);
                rM++;

                for (int c = 0; c < resultado.MuestraArchivo2.Encabezados.Count; c++)
                {
                    wsM.Cell(rM, c + 1).Value = resultado.MuestraArchivo2.Encabezados[c];
                    wsM.Cell(rM, c + 1).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.LightGray);
                }
                rM++;

                for (int i = 0; i < resultado.MuestraArchivo2.Filas.Count; i++)
                {
                    for (int c = 0; c < resultado.MuestraArchivo2.Filas[i].Count; c++)
                    {
                        wsM.Cell(rM, c + 1).Value = resultado.MuestraArchivo2.Filas[i][c];
                        wsM.Cell(rM, c + 1).Style.Fill.SetBackgroundColor(XLColor.White);
                    }
                    rM++;
                }
            }
            wsM.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }
    }

    // ==========================================
    // MODELOS PARA VALIDACIÓN ESTRUCTURAL
    // ==========================================
    public class ResultadoEstructura
    {
        public MetadatosArchivo Archivo1 { get; set; } = new MetadatosArchivo();
        public MetadatosArchivo Archivo2 { get; set; } = new MetadatosArchivo();
        public MuestraDatos MuestraArchivo1 { get; set; } = new MuestraDatos();
        public MuestraDatos MuestraArchivo2 { get; set; } = new MuestraDatos();
        public List<ComparacionColumna> ComparacionColumnas { get; set; } = new List<ComparacionColumna>();
        public List<string> AdvertenciasFisicas { get; set; } = new List<string>();
        public bool EstructurasCoinciden { get; set; }
        public int ColumnasFaltantes { get; set; }
        public int TiposDiferentes { get; set; }
    }

    public class MuestraDatos
    {
        public List<string> Encabezados { get; set; } = new List<string>();
        public List<List<string>> Filas { get; set; } = new List<List<string>>();
    }

    public class MetadatosArchivo
    {
        public string Alias { get; set; }
        public string NombreArchivo { get; set; }
        public string Extension { get; set; }
        public string PesoArchivo { get; set; }
        public string SaltoLinea { get; set; }
        public string Separador { get; set; }
        public string Encoding { get; set; }
        public int TotalColumnas { get; set; }
        public int TotalFilasMuestra { get; set; }
        public List<ColumnaInfo> Columnas { get; set; } = new List<ColumnaInfo>();
    }

    public class ColumnaInfo
    {
        public string Nombre { get; set; }
        public string TipoInferido { get; set; }
        public int LongitudMaxima { get; set; }
        public int LongitudMinima { get; set; }
        public bool TieneCerosALaIzquierda { get; set; }
        public int NulosDetectados { get; set; }
        public double PorcentajeNulos { get; set; }
    }

    public class ComparacionColumna
    {
        public string NombreColumna { get; set; }
        public string TipoArchivo1 { get; set; }
        public string TipoArchivo2 { get; set; }
        public string Estado { get; set; }
        public ColumnaInfo MetadatosColumnaA { get; set; }
        public ColumnaInfo MetadatosColumnaB { get; set; }
    }
}
