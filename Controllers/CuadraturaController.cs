using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Data;
using System.Collections.Generic;
using NotebookValidator.Web.Services;
using NotebookValidator.Web.Models;
using NotebookValidator.Web.Data;
using System;
using System.Text.Json;
using System.Globalization;
using ExcelDataReader;
using System.IO.Compression;

namespace NotebookValidator.Web.Controllers
{
    [Authorize]
    public class CuadraturaController : Controller
    {
        private readonly ICuadraturaService _cuadraturaService;
        private readonly AuditService _auditService;
        private readonly UserManager<ApplicationUser> _userManager;

        public CuadraturaController(
            ICuadraturaService cuadraturaService,
            AuditService auditService,
            UserManager<ApplicationUser> userManager)
        {
            _cuadraturaService = cuadraturaService;
            _auditService = auditService;
            _userManager = userManager;
        }

        public IActionResult Index() => View();

        [HttpPost]
        public IActionResult ObtenerHojasExcel(IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0) return Json(new string[0]);
            var ext = Path.GetExtension(archivo.FileName).ToLower();
            if (ext == ".csv" || ext == ".txt" || ext == ".dat") return Json(new[] { "Única" });

            try
            {
                using var stream = archivo.OpenReadStream();
                using var reader = ExcelReaderFactory.CreateReader(stream);
                var hojas = new List<string>();
                do { hojas.Add(reader.Name); } while (reader.NextResult());
                return Json(hojas);
            }
            catch { return Json(new string[0]); }
        }

        [HttpPost]
        public async Task<IActionResult> CargarArchivos(IFormFile archivo1, IFormFile archivo2, string alias1, string alias2, bool tieneEncabezados1 = false, bool tieneEncabezados2 = false, string hoja1 = null, string hoja2 = null)
        {
            if (archivo1 == null || archivo2 == null) return View("Index");
            var ext1 = Path.GetExtension(archivo1.FileName); var ext2 = Path.GetExtension(archivo2.FileName);
            var tempPath1 = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ext1);
            var tempPath2 = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ext2);

            using (var stream = new FileStream(tempPath1, FileMode.Create)) await archivo1.CopyToAsync(stream);
            using (var stream = new FileStream(tempPath2, FileMode.Create)) await archivo2.CopyToAsync(stream);

            DataTable dt1 = _cuadraturaService.LeerExcel(tempPath1, tieneEncabezados1, hoja1);
            DataTable dt2 = _cuadraturaService.LeerExcel(tempPath2, tieneEncabezados2, hoja2);

            var distinct1 = new Dictionary<string, int>();
            foreach (DataColumn col in dt1.Columns)
            {
                var uniqueVals = new HashSet<string>();
                foreach (DataRow row in dt1.Rows) uniqueVals.Add(row[col]?.ToString() ?? "");
                distinct1[col.ColumnName] = uniqueVals.Count;
            }

            var distinct2 = new Dictionary<string, int>();
            foreach (DataColumn col in dt2.Columns)
            {
                var uniqueVals = new HashSet<string>();
                foreach (DataRow row in dt2.Rows) uniqueVals.Add(row[col]?.ToString() ?? "");
                distinct2[col.ColumnName] = uniqueVals.Count;
            }

            var viewModel = new MapeoColumnasViewModel
            {
                AliasArchivo1 = string.IsNullOrWhiteSpace(alias1) ? "Archivo 1" : alias1,
                AliasArchivo2 = string.IsNullOrWhiteSpace(alias2) ? "Archivo 2" : alias2,
                HojaArchivo1 = hoja1,
                HojaArchivo2 = hoja2,
                ColumnasArchivo1 = dt1.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList(),
                ColumnasArchivo2 = dt2.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList(),
                Sugerencias = _cuadraturaService.InferirColumnas(dt1, dt2),
                TempPathArchivo1 = tempPath1,
                TempPathArchivo2 = tempPath2,
                TieneEncabezados1 = tieneEncabezados1,
                TieneEncabezados2 = tieneEncabezados2,
                DistinctCountsArchivo1 = distinct1,
                DistinctCountsArchivo2 = distinct2
            };
            return View("MapeoColumnas", viewModel);
        }

        [HttpPost]
        public IActionResult ObtenerPrevisualizacion(string tempPath, bool tieneEncabezados, string nombreHoja)
        {
            if (string.IsNullOrEmpty(tempPath) || !System.IO.File.Exists(tempPath)) return BadRequest();
            DataTable dt = _cuadraturaService.LeerExcel(tempPath, tieneEncabezados, nombreHoja);

            var headers = dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
            var rows = new List<List<string>>();

            foreach (DataRow row in dt.AsEnumerable().Take(5))
            {
                rows.Add(row.ItemArray.Select(i => i?.ToString() ?? "").ToList());
            }

            return Json(new { headers, rows });
        }

        [HttpPost]
        public async Task<IActionResult> EjecutarCuadratura(string TempPathArchivo1, string TempPathArchivo2, string AliasArchivo1, string AliasArchivo2, bool TieneEncabezados1, bool TieneEncabezados2, string HojaArchivo1, string HojaArchivo2, bool ModoAgrupacion, List<string> LlaveArchivo1, List<string> LlaveArchivo2, List<string> ColsComparar1, List<string> ColsComparar2, List<string> Tolerancias)
        {
            DataTable dt1 = _cuadraturaService.LeerExcel(TempPathArchivo1, TieneEncabezados1, HojaArchivo1);
            DataTable dt2 = _cuadraturaService.LeerExcel(TempPathArchivo2, TieneEncabezados2, HojaArchivo2);

            int countOriginal1 = dt1.Rows.Count;
            int countOriginal2 = dt2.Rows.Count;

            var validCols1 = new List<string>();
            var validCols2 = new List<string>();
            var validTols = new List<double>();

            if (ColsComparar1 != null && ColsComparar2 != null)
            {
                for (int i = 0; i < Math.Min(ColsComparar1.Count, ColsComparar2.Count); i++)
                {
                    if (!string.IsNullOrEmpty(ColsComparar1[i]) && !string.IsNullOrEmpty(ColsComparar2[i]))
                    {
                        validCols1.Add(ColsComparar1[i]);
                        validCols2.Add(ColsComparar2[i]);
                        double tol = 0.0001;
                        if (Tolerancias != null && Tolerancias.Count > i && !string.IsNullOrWhiteSpace(Tolerancias[i]))
                        {
                            if (double.TryParse(Tolerancias[i].Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedTol))
                                tol = parsedTol;
                        }
                        validTols.Add(tol);
                    }
                }
            }

            if (ModoAgrupacion)
            {
                dt1 = _cuadraturaService.AgruparDataTable(dt1, LlaveArchivo1, validCols1);
                dt2 = _cuadraturaService.AgruparDataTable(dt2, LlaveArchivo2, validCols2);
            }

            var resultados = _cuadraturaService.CompararDatos(dt1, dt2, LlaveArchivo1, LlaveArchivo2, validCols1, validCols2, validTols);
            resultados.AliasArchivo1 = string.IsNullOrWhiteSpace(AliasArchivo1) ? "Archivo 1" : AliasArchivo1;
            resultados.AliasArchivo2 = string.IsNullOrWhiteSpace(AliasArchivo2) ? "Archivo 2" : AliasArchivo2;
            resultados.EsModoAgrupacion = ModoAgrupacion;
            resultados.LlavesAgrupacion = LlaveArchivo1 ?? new List<string>();
            resultados.TotalOriginal1 = countOriginal1;
            resultados.TotalOriginal2 = countOriginal2;
            resultados.TotalAgrupado1 = dt1.Rows.Count;
            resultados.TotalAgrupado2 = dt2.Rows.Count;

            var userId = _userManager.GetUserId(User);
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var auditDetails = new { Modulo = "Cuadratura", Accion = "Comparación Ejecutada", Archivos = new { Origen1 = resultados.AliasArchivo1, Origen2 = resultados.AliasArchivo2 } };
            await _auditService.LogActionAsync(userId, "Cuadratura: Comparación Ejecutada", JsonSerializer.Serialize(auditDetails), ip);

            // ========================================================
            // GUARDADO TEMPORAL PARA QUE EXPORTAR LO LEA DESPUÉS
            // ========================================================
            string exportId = Guid.NewGuid().ToString();

            // 1. Estructura
            var resultadoEstructura = _cuadraturaService.ValidarEstructuras(TempPathArchivo1, TempPathArchivo2, resultados.AliasArchivo1, resultados.AliasArchivo2, TieneEncabezados1, TieneEncabezados2, HojaArchivo1, HojaArchivo2);
            resultadoEstructura.Archivo1.NombreArchivo = resultados.AliasArchivo1;
            resultadoEstructura.Archivo2.NombreArchivo = resultados.AliasArchivo2;

            // 2. Guardar JSONs (¡CORREGIDO EL PUNTO FALTANTE AQUÍ!)
            System.IO.File.WriteAllText(Path.Combine(Path.GetTempPath(), exportId + ".json"), JsonSerializer.Serialize(resultados));
            System.IO.File.WriteAllText(Path.Combine(Path.GetTempPath(), exportId + "_estruct.json"), JsonSerializer.Serialize(resultadoEstructura));

            // 3. Guardar las tablas crudas para pegarlas en la fila 29
            dt1.TableName = "dt1"; dt2.TableName = "dt2";
            dt1.WriteXml(Path.Combine(Path.GetTempPath(), exportId + "_dt1.xml"), XmlWriteMode.WriteSchema);
            dt2.WriteXml(Path.Combine(Path.GetTempPath(), exportId + "_dt2.xml"), XmlWriteMode.WriteSchema);

            if (System.IO.File.Exists(TempPathArchivo1)) System.IO.File.Delete(TempPathArchivo1);
            if (System.IO.File.Exists(TempPathArchivo2)) System.IO.File.Delete(TempPathArchivo2);

            ViewBag.ExportId = exportId;
            return View("Resultados", resultados);
        }

        [HttpPost]
        public IActionResult ExportarExcel(string exportId, string base64Image)
        {
            if (string.IsNullOrEmpty(exportId)) return RedirectToAction("Index");

            string jsonPath = Path.Combine(Path.GetTempPath(), exportId + ".json");
            string estructPath = Path.Combine(Path.GetTempPath(), exportId + "_estruct.json");
            string xml1 = Path.Combine(Path.GetTempPath(), exportId + "_dt1.xml");
            string xml2 = Path.Combine(Path.GetTempPath(), exportId + "_dt2.xml");

            if (!System.IO.File.Exists(jsonPath) || !System.IO.File.Exists(xml1)) return RedirectToAction("Index");

            try
            {
                var resultados = JsonSerializer.Deserialize<ResultadoCuadratura>(System.IO.File.ReadAllText(jsonPath));
                var resultadoEstructura = JsonSerializer.Deserialize<ResultadoEstructura>(System.IO.File.ReadAllText(estructPath));

                DataTable dt1 = new DataTable(); dt1.ReadXml(xml1);
                DataTable dt2 = new DataTable(); dt2.ReadXml(xml2);

                // Pasamos la imagen generada en la web app al reporteador
                var excelCuadratura = _cuadraturaService.GenerarExcelReporte(resultados, dt1, dt2, base64Image);
                var excelEstructura = _cuadraturaService.GenerarExcelReporteEstructura(resultadoEstructura);

                string zipPath = Path.Combine(Path.GetTempPath(), exportId + ".zip");
                using (var fileStream = new FileStream(zipPath, FileMode.Create))
                {
                    using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Create, true))
                    {
                        var entry1 = archive.CreateEntry($"Reporte_Cuadratura_Datos_{DateTime.Now:yyyyMMddHHmm}.xlsx");
                        using (var entryStream = entry1.Open()) { entryStream.Write(excelCuadratura, 0, excelCuadratura.Length); }

                        var entry2 = archive.CreateEntry($"Reporte_Validacion_Estructura_{DateTime.Now:yyyyMMddHHmm}.xlsx");
                        using (var entryStream = entry2.Open()) { entryStream.Write(excelEstructura, 0, excelEstructura.Length); }
                    }
                }

                var zipBytes = System.IO.File.ReadAllBytes(zipPath);

                // Limpieza
                System.IO.File.Delete(jsonPath); System.IO.File.Delete(estructPath);
                System.IO.File.Delete(xml1); System.IO.File.Delete(xml2); System.IO.File.Delete(zipPath);

                return File(zipBytes, "application/zip", $"Reportes_Cuadratura_{DateTime.Now:yyyyMMddHHmm}.zip");
            }
            catch { return RedirectToAction("Index"); }
        }

        [HttpPost]
        public async Task<IActionResult> ValidarEstructuras(IFormFile archivo1, IFormFile archivo2, string alias1, string alias2, bool tieneEncabezados1 = false, bool tieneEncabezados2 = false, string hoja1 = null, string hoja2 = null)
        {
            if (archivo1 == null || archivo2 == null) return View("Index");

            var ext1 = Path.GetExtension(archivo1.FileName);
            var ext2 = Path.GetExtension(archivo2.FileName);
            var tempPath1 = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ext1);
            var tempPath2 = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ext2);

            using (var stream = new FileStream(tempPath1, FileMode.Create)) await archivo1.CopyToAsync(stream);
            using (var stream = new FileStream(tempPath2, FileMode.Create)) await archivo2.CopyToAsync(stream);

            var resultado = _cuadraturaService.ValidarEstructuras(tempPath1, tempPath2, alias1, alias2, tieneEncabezados1, tieneEncabezados2, hoja1, hoja2);

            resultado.Archivo1.NombreArchivo = archivo1.FileName;
            resultado.Archivo2.NombreArchivo = archivo2.FileName;

            // --- REGISTRO DE AUDITORÍA DETALLADO Y HERMOSO ---
            var userId = _userManager.GetUserId(User);
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var auditDetails = new
            {
                Modulo = "Cuadratura de Datos",
                Accion = "Validación Estructural Ejecutada",
                Analisis = new
                {
                    ArchivoA = new { Alias = resultado.Archivo1.Alias, Archivo = resultado.Archivo1.NombreArchivo, Extension = resultado.Archivo1.Extension, Columnas = resultado.Archivo1.TotalColumnas },
                    ArchivoB = new { Alias = resultado.Archivo2.Alias, Archivo = resultado.Archivo2.NombreArchivo, Extension = resultado.Archivo2.Extension, Columnas = resultado.Archivo2.TotalColumnas }
                },
                MetadatosFisicos = new
                {
                    DiferenciasEncontradas = resultado.AdvertenciasFisicas.Count,
                    ListaDiferencias = resultado.AdvertenciasFisicas
                },
                ResultadoLogico = new
                {
                    EsquemasCoinciden = resultado.EstructurasCoinciden,
                    ColumnasFaltantes = resultado.ColumnasFaltantes,
                    ConflictosDeTipo = resultado.TiposDiferentes
                }
            };

            await _auditService.LogActionAsync(userId, "Cuadratura: Validación Estructural", JsonSerializer.Serialize(auditDetails, new JsonSerializerOptions { WriteIndented = true }), ip);

            if (System.IO.File.Exists(tempPath1)) System.IO.File.Delete(tempPath1);
            if (System.IO.File.Exists(tempPath2)) System.IO.File.Delete(tempPath2);

            string exportId = Guid.NewGuid().ToString();
            string tempJsonPath = Path.Combine(Path.GetTempPath(), exportId + "_estruct.json");
            System.IO.File.WriteAllText(tempJsonPath, JsonSerializer.Serialize(resultado));
            ViewBag.ExportId = exportId;

            return View("ResultadosEstructura", resultado);
        }

        [HttpPost]
        public async Task<IActionResult> ExportarExcelEstructura(string exportId)
        {
            if (string.IsNullOrEmpty(exportId)) return RedirectToAction("Index");
            string tempJsonPath = Path.Combine(Path.GetTempPath(), exportId + "_estruct.json");
            if (!System.IO.File.Exists(tempJsonPath)) return RedirectToAction("Index");

            try
            {
                var jsonResultado = System.IO.File.ReadAllText(tempJsonPath);
                var resultado = JsonSerializer.Deserialize<ResultadoEstructura>(jsonResultado);
                var archivo = _cuadraturaService.GenerarExcelReporteEstructura(resultado);
                System.IO.File.Delete(tempJsonPath);

                // --- LOG AUDITORÍA EXPORTACIÓN ---
                var userId = _userManager.GetUserId(User);
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                var exportAudit = new { Modulo = "Cuadratura de Datos", Accion = "Descarga Excel", Archivos = $"{resultado.Archivo1.Alias} vs {resultado.Archivo2.Alias}" };
                await _auditService.LogActionAsync(userId, "Cuadratura: Exportación Excel Estructura", JsonSerializer.Serialize(exportAudit), ip);

                return File(archivo, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Reporte_Estructura_{DateTime.Now:yyyyMMddHHmm}.xlsx");
            }
            catch { return RedirectToAction("Index"); }
        }
    }
}
