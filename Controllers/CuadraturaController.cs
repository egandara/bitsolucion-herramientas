using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Data;
using System.Collections.Generic;
using NotebookValidator.Web.Services;
using NotebookValidator.Web.Models;
using System;
using System.Text.Json;
using System.Globalization;

namespace NotebookValidator.Web.Controllers
{
    public class CuadraturaController : Controller
    {
        private readonly ICuadraturaService _cuadraturaService;

        public CuadraturaController(ICuadraturaService cuadraturaService)
        {
            _cuadraturaService = cuadraturaService;
        }

        public IActionResult Index() => View();

        [HttpPost]
        public async Task<IActionResult> CargarArchivos(IFormFile archivo1, IFormFile archivo2, string alias1, string alias2, bool tieneEncabezados1 = false, bool tieneEncabezados2 = false)
        {
            if (archivo1 == null || archivo2 == null) return View("Index");
            var ext1 = Path.GetExtension(archivo1.FileName); var ext2 = Path.GetExtension(archivo2.FileName);
            var tempPath1 = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ext1);
            var tempPath2 = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ext2);

            using (var stream = new FileStream(tempPath1, FileMode.Create)) await archivo1.CopyToAsync(stream);
            using (var stream = new FileStream(tempPath2, FileMode.Create)) await archivo2.CopyToAsync(stream);

            DataTable dt1 = _cuadraturaService.LeerExcel(tempPath1, tieneEncabezados1);
            DataTable dt2 = _cuadraturaService.LeerExcel(tempPath2, tieneEncabezados2);

            var viewModel = new MapeoColumnasViewModel
            {
                AliasArchivo1 = string.IsNullOrWhiteSpace(alias1) ? "Archivo 1" : alias1,
                AliasArchivo2 = string.IsNullOrWhiteSpace(alias2) ? "Archivo 2" : alias2,
                ColumnasArchivo1 = dt1.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList(),
                ColumnasArchivo2 = dt2.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList(),
                Sugerencias = _cuadraturaService.InferirColumnas(dt1, dt2),
                TempPathArchivo1 = tempPath1,
                TempPathArchivo2 = tempPath2,
                TieneEncabezados1 = tieneEncabezados1,
                TieneEncabezados2 = tieneEncabezados2
            };
            return View("MapeoColumnas", viewModel);
        }

        [HttpPost]
        public IActionResult ObtenerPrevisualizacion(string tempPath, bool tieneEncabezados)
        {
            if (string.IsNullOrEmpty(tempPath) || !System.IO.File.Exists(tempPath)) return BadRequest();
            DataTable dt = _cuadraturaService.LeerExcel(tempPath, tieneEncabezados);

            var headers = dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
            var rows = new List<List<string>>();

            foreach (DataRow row in dt.AsEnumerable().Take(5))
            {
                rows.Add(row.ItemArray.Select(i => i?.ToString() ?? "").ToList());
            }

            return Json(new { headers, rows });
        }

        [HttpPost]
        public IActionResult EjecutarCuadratura(string TempPathArchivo1, string TempPathArchivo2, string AliasArchivo1, string AliasArchivo2, bool TieneEncabezados1, bool TieneEncabezados2, bool ModoAgrupacion, List<string> LlaveArchivo1, List<string> LlaveArchivo2, List<string> ColsComparar1, List<string> ColsComparar2, List<string> Tolerancias)
        {
            DataTable dt1 = _cuadraturaService.LeerExcel(TempPathArchivo1, TieneEncabezados1);
            DataTable dt2 = _cuadraturaService.LeerExcel(TempPathArchivo2, TieneEncabezados2);

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
                            {
                                tol = parsedTol;
                            }
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

            if (System.IO.File.Exists(TempPathArchivo1)) System.IO.File.Delete(TempPathArchivo1);
            if (System.IO.File.Exists(TempPathArchivo2)) System.IO.File.Delete(TempPathArchivo2);

            // Guardar resultado temporal para exportación
            string exportId = Guid.NewGuid().ToString();
            string tempJsonPath = Path.Combine(Path.GetTempPath(), exportId + ".json");
            System.IO.File.WriteAllText(tempJsonPath, JsonSerializer.Serialize(resultados));
            ViewBag.ExportId = exportId;

            return View("Resultados", resultados);
        }

        [HttpPost]
        public IActionResult ExportarExcel(string exportId)
        {
            if (string.IsNullOrEmpty(exportId)) return RedirectToAction("Index");
            string tempJsonPath = Path.Combine(Path.GetTempPath(), exportId + ".json");
            if (!System.IO.File.Exists(tempJsonPath)) return RedirectToAction("Index");

            try
            {
                var jsonResultado = System.IO.File.ReadAllText(tempJsonPath);
                var resultado = JsonSerializer.Deserialize<ResultadoCuadratura>(jsonResultado);
                var archivo = _cuadraturaService.GenerarExcelReporte(resultado);
                System.IO.File.Delete(tempJsonPath);

                return File(archivo, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Reporte_Cuadratura_{DateTime.Now:yyyyMMddHHmm}.xlsx");
            }
            catch { return RedirectToAction("Index"); }
        }
    }
}
