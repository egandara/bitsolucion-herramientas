using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using NotebookValidator.Web.Data;
using NotebookValidator.Web.Models;
using NotebookValidator.Web.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace NotebookValidator.Web.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly NotebookValidatorService _validatorService;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly AuditService _auditService;

        public HomeController(
            NotebookValidatorService validatorService,
            IWebHostEnvironment hostEnvironment,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            AuditService auditService)
        {
            _validatorService = validatorService;
            _hostEnvironment = hostEnvironment;
            _userManager = userManager;
            _context = context;
            _auditService = auditService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new List<Finding>());
        }

        [HttpPost]
        public async Task<IActionResult> Validate(IFormFileCollection files)
        {
            if (files == null || files.Count == 0)
            {
                return Json(new { summary = new Dictionary<string, int>(), findings = new List<Finding>(), hasResults = false });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var notebooksToProcess = new List<(string tempPath, string originalName)>();
            var tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDirectory);

            try
            {
                // 1. Descomprimir ZIPs y crear una lista plana de todos los notebooks.
                foreach (var file in files)
                {
                    var tempFilePath = Path.Combine(tempDirectory, file.FileName);
                    using (var stream = new FileStream(tempFilePath, FileMode.Create)) { await file.CopyToAsync(stream); }

                    if (Path.GetExtension(file.FileName).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        var extractPath = Path.Combine(tempDirectory, Guid.NewGuid().ToString());
                        Directory.CreateDirectory(extractPath);
                        ZipFile.ExtractToDirectory(tempFilePath, extractPath);
                        var notebookFilesInZip = Directory.GetFiles(extractPath, "*.*", SearchOption.AllDirectories)
                            .Where(f => f.EndsWith(".py") || f.EndsWith(".ipynb"));
                        foreach (var notebookPath in notebookFilesInZip)
                        {
                            notebooksToProcess.Add((notebookPath, Path.GetFileName(notebookPath)));
                        }
                    }
                    else if (file.FileName.EndsWith(".py") || file.FileName.EndsWith(".ipynb"))
                    {
                        notebooksToProcess.Add((tempFilePath, file.FileName));
                    }
                }

                // 2. Verificar la cuota.
                if (user.AnalysisQuota < notebooksToProcess.Count)
                {
                    return Json(new
                    {
                        summary = new Dictionary<string, string> { { "Error de Cuota", $"Créditos insuficientes ({user.AnalysisQuota}) para analizar {notebooksToProcess.Count} archivos." } },
                        findings = new List<Finding>(),
                        hasResults = true
                    });
                }

                // 3. Procesar cada notebook.
                var allFindings = new List<Finding>();
                foreach (var (tempPath, originalName) in notebooksToProcess)
                {
                    var findings = await _validatorService.AnalyzeNotebookAsync(tempPath, originalName);
                    allFindings.AddRange(findings);
                }

                // 4. Descontar y guardar la nueva cuota.
                user.AnalysisQuota -= notebooksToProcess.Count;
                await _userManager.UpdateAsync(user);

                // 5. Guardar el registro del análisis en el historial.
                var analysisRun = new AnalysisRun
                {
                    UserId = user.Id,
                    AnalysisTimestamp = DateTime.Now,
                    TotalFilesAnalyzed = notebooksToProcess.Count,
                    TotalProblemsFound = allFindings.Count, // <-- Usamos esta propiedad
                    ResultsJson = JsonSerializer.Serialize(allFindings)
                };
                _context.AnalysisRuns.Add(analysisRun);
                await _context.SaveChangesAsync();

                // === INICIO REGISTRO DE AUDITORÍA (AÑADIR AQUÍ) ===
                var fileNamesForAudit = string.Join(", ", files.Select(f => f.FileName));
                var details = $"Ejecutó Validador de Notebooks para los archivos: [{fileNamesForAudit}]. Hallazgos: {analysisRun.TotalProblemsFound}.";
                await _auditService.LogActionAsync(user.Id, "NotebookValidation", details, analysisRun.Id.ToString());
                // === FIN REGISTRO DE AUDITORÍA ===

                // 6. Preparar y devolver la respuesta JSON.
                var summaryData = allFindings
                    .GroupBy(f => new { f.FindingType, f.Severity })
                    .ToDictionary(
                        g => g.Key.FindingType,
                        g => new { Count = g.Count(), Severity = g.Key.Severity }
                    );

                HttpContext.Session.SetString("ValidationResults", JsonSerializer.Serialize(allFindings));

                return Json(new { summary = summaryData, findings = allFindings, hasResults = allFindings.Any() });
            }
            finally
            {
                if (Directory.Exists(tempDirectory))
                {
                    Directory.Delete(tempDirectory, recursive: true);
                }
            }
        }

        public async Task<IActionResult> ExportToExcel(int? analysisId)
        {
            var jsonResults = HttpContext.Session.GetString("ValidationResults");
            if (string.IsNullOrEmpty(jsonResults))
            {
                // Si la sesión está vacía, no podemos hacer nada.
                return RedirectToAction("Index");
            }

            var findings = JsonSerializer.Deserialize<List<Finding>>(jsonResults);
            var user = await _userManager.GetUserAsync(User);
            AnalysisRun analysisRunToExport = null;

            if (analysisId.HasValue)
            {
                // --- LÓGICA NUEVA: Exportando desde el Historial ---
                // Busca el reporte específico que se está viendo.
                analysisRunToExport = await _context.AnalysisRuns
                    .Include(r => r.User) // Incluimos el usuario para obtener su email
                    .FirstOrDefaultAsync(r => r.Id == analysisId.Value);

                // Comprobación de seguridad: (Opcional) Un admin puede exportar todo.
                // Si no es admin, verifica que sea su propio reporte.
                bool isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                if (!isAdmin && analysisRunToExport.UserId != user.Id)
                {
                    return Unauthorized("No tienes permiso para exportar este reporte.");
                }
            }
            else
            {
                // --- LÓGICA ORIGINAL: Exportando desde Home/Index ---
                // Busca el último reporte del usuario actual.
                analysisRunToExport = await _context.AnalysisRuns
                    .Where(r => r.UserId == user.Id)
                    .OrderByDescending(r => r.AnalysisTimestamp)
                    .FirstOrDefaultAsync();

                // Asignamos el usuario actual (ya que es su propio reporte)
                if (analysisRunToExport != null)
                {
                    analysisRunToExport.User = user;
                }
            }

            if (analysisRunToExport == null)
            {
                // Si por alguna razón no encontramos un reporte (ej. usuario nuevo sin análisis),
                // creamos uno temporal solo para el encabezado.
                analysisRunToExport = new AnalysisRun
                {
                    Id = 0,
                    User = user,
                    AnalysisTimestamp = DateTime.Now
                };
            }

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Reporte de Validación");

                // Encabezado
                var logoPath = Path.Combine(_hostEnvironment.WebRootPath, "img", "Bit-solucion-logo-menu.png");
                if (System.IO.File.Exists(logoPath))
                {
                    worksheet.AddPicture(logoPath).MoveTo(worksheet.Cell("A1")).Scale(0.5);
                }
                worksheet.Range("A1:B3").Style.Fill.BackgroundColor = XLColor.FromHtml("#07091e");
                worksheet.Cell("C2").Value = "Reporte de Análisis de Notebooks";
                worksheet.Cell("C2").Style.Font.Bold = true;
                worksheet.Cell("C2").Style.Font.FontSize = 16;
                worksheet.Range("C2:F2").Merge();

                // --- USA LOS DATOS DEL REPORTE CORRECTO ---
                worksheet.Cell("C4").Value = "Análisis ID:";
                worksheet.Cell("D4").Value = analysisRunToExport.Id;
                worksheet.Cell("C5").Value = "Usuario:";
                worksheet.Cell("D5").Value = analysisRunToExport.User.Email;
                worksheet.Cell("C6").Value = "Fecha:";
                worksheet.Cell("D6").Value = analysisRunToExport.AnalysisTimestamp.ToString("g");

                // Tabla de Resumen
                var summaryHeaderRow = 11;
                worksheet.Cell("A9").Value = "Resumen por Archivo y Tipo de Problema";
                worksheet.Cell("A9").Style.Font.Bold = true;

                // Usamos los 'findings' de la sesión (que son los correctos)
                var allProblemTypes = findings.Select(f => f.FindingType).Distinct().OrderBy(t => t).ToList();
                var allFiles = findings.Select(f => f.FileName).Distinct().OrderBy(f => f).ToList();

                worksheet.Cell(summaryHeaderRow, 1).Value = "Notebook Analizado";
                for (int i = 0; i < allProblemTypes.Count; i++)
                {
                    worksheet.Cell(summaryHeaderRow, 2 + i).Value = allProblemTypes[i];
                }
                for (int i = 0; i < allFiles.Count; i++)
                {
                    var fileName = allFiles[i];
                    worksheet.Cell(summaryHeaderRow + 1 + i, 1).Value = fileName;
                    for (int j = 0; j < allProblemTypes.Count; j++)
                    {
                        var problemType = allProblemTypes[j];
                        var count = findings.Count(f => f.FileName == fileName && f.FindingType == problemType);
                        worksheet.Cell(summaryHeaderRow + 1 + i, 2 + j).Value = count;
                    }
                }
                var summaryTableRange = worksheet.Range(summaryHeaderRow, 1, summaryHeaderRow + allFiles.Count, 1 + allProblemTypes.Count);
                summaryTableRange.Style.Border.SetInsideBorder(XLBorderStyleValues.Thin);
                summaryTableRange.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                var summaryHeader = worksheet.Range(summaryHeaderRow, 1, summaryHeaderRow, 1 + allProblemTypes.Count);
                summaryHeader.Style.Fill.BackgroundColor = XLColor.FromHtml("#0A192F");
                summaryHeader.Style.Font.FontColor = XLColor.White;

                // Tabla de Detalles
                var detailsHeaderRow = summaryHeaderRow + allFiles.Count + 3;
                worksheet.Cell(detailsHeaderRow, 1).Value = "Resultados Detallados";
                worksheet.Cell(detailsHeaderRow, 1).Style.Font.Bold = true;

                // Creamos una versión "plana" de los findings para la tabla, excluyendo el código fuente
                var findingsForTable = findings.Select(f => new {
                    f.FileName,
                    f.FindingType,
                    f.Severity,
                    f.CellNumber,
                    f.LineNumber,
                    f.Content,
                    f.Details
                }).ToList();

                worksheet.Cell(detailsHeaderRow + 2, 1).InsertTable(findingsForTable);
                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Reporte_Validacion_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
                }
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}