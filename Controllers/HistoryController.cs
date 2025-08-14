using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotebookValidator.Web.Data;
using NotebookValidator.Web.Models;
using NotebookValidator.Web.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ClosedXML.Excel;

namespace NotebookValidator.Web.Controllers
{
    [Authorize(Roles = "Admin, ValidatorUser")]
    public class HistoryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _hostEnvironment;

        public HistoryController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _hostEnvironment = hostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var userHistory = await _context.AnalysisRuns
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.AnalysisTimestamp)
                .ToListAsync();
            var allFindings = userHistory
                .SelectMany(run => JsonSerializer.Deserialize<List<Finding>>(run.ResultsJson) ?? new List<Finding>())
                .ToList();
            var problemTypeCounts = allFindings
                .GroupBy(f => f.FindingType)
                .ToDictionary(g => g.Key, g => g.Count());
            var mostCommonProblem = problemTypeCounts.Any() 
                ? problemTypeCounts.OrderByDescending(kvp => kvp.Value).First().Key 
                : "N/A";
            var viewModel = new HistoryDashboardViewModel
            {
                TotalAnalyses = userHistory.Count,
                TotalFilesAnalyzed = userHistory.Sum(r => r.TotalFilesAnalyzed),
                TotalProblemsFound = userHistory.Sum(r => r.TotalProblemsFound),
                MostCommonProblem = mostCommonProblem,
                ProblemTypeCounts = problemTypeCounts,
                AnalysisRuns = userHistory
            };
            return View(viewModel);
        }

        public async Task<IActionResult> Details(int id)
        {
            var userId = _userManager.GetUserId(User);
            var analysisRun = await _context.AnalysisRuns.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
            if (analysisRun == null) return NotFound();
            
            ViewBag.AnalysisRunId = id;
            var findings = JsonSerializer.Deserialize<List<Finding>>(analysisRun.ResultsJson);
            return View(findings);
        }

        public async Task<IActionResult> ExportHistoryToExcel(int id)
        {
            var userId = _userManager.GetUserId(User);
            var analysisRun = await _context.AnalysisRuns
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
            if (analysisRun == null) return NotFound();

            var findings = JsonSerializer.Deserialize<List<Finding>>(analysisRun.ResultsJson);

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Reporte de Validación");

                // --- 1. Encabezado ---
                var logoPath = Path.Combine(_hostEnvironment.WebRootPath, "img", "Bit-solucion-logo-menu.png");
                if (System.IO.File.Exists(logoPath))
                {
                    worksheet.AddPicture(logoPath).MoveTo(worksheet.Cell("A1")).Scale(0.5);
                }
                // NUEVO: Color de fondo para el logo
                worksheet.Range("A1:B3").Style.Fill.BackgroundColor = XLColor.FromHtml("#07091e");

                worksheet.Cell("C2").Value = "Reporte de Análisis de Notebooks";
                worksheet.Cell("C2").Style.Font.Bold = true;
                worksheet.Cell("C2").Style.Font.FontSize = 16;
                worksheet.Range("C2:F2").Merge();
                worksheet.Cell("C4").Value = "Análisis ID:";
                worksheet.Cell("D4").Value = analysisRun.Id;
                worksheet.Cell("C5").Value = "Usuario:";
                worksheet.Cell("D5").Value = analysisRun.User.Email;
                worksheet.Cell("C6").Value = "Fecha:";
                worksheet.Cell("D6").Value = analysisRun.AnalysisTimestamp.ToString("g");

                // --- 2. Tabla de Resumen ---
                var summaryHeaderRow = 11;
                worksheet.Cell("A9").Value = "Resumen por Archivo y Tipo de Problema";
                worksheet.Cell("A9").Style.Font.Bold = true;
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

                // NUEVO: Formato para la tabla de resumen
                var summaryTableRange = worksheet.Range(summaryHeaderRow, 1, summaryHeaderRow + allFiles.Count, 1 + allProblemTypes.Count);
                summaryTableRange.Style.Border.SetInsideBorder(XLBorderStyleValues.Thin);
                summaryTableRange.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                var summaryHeader = worksheet.Range(summaryHeaderRow, 1, summaryHeaderRow, 1 + allProblemTypes.Count);
                summaryHeader.Style.Fill.BackgroundColor = XLColor.FromHtml("#0A192F");
                summaryHeader.Style.Font.FontColor = XLColor.White;

                // --- 3. Tabla de Detalles ---
                var detailsHeaderRow = summaryHeaderRow + allFiles.Count + 3;
                worksheet.Cell(detailsHeaderRow, 1).Value = "Resultados Detallados";
                worksheet.Cell(detailsHeaderRow, 1).Style.Font.Bold = true;
                worksheet.Cell(detailsHeaderRow + 2, 1).InsertTable(findings);

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Reporte_Historial_{id}_{analysisRun.AnalysisTimestamp:yyyyMMdd}.xlsx");
                }
            }
        }

        public async Task<IActionResult> DownloadHistoryAsPdf(int id)
        {
            var userId = _userManager.GetUserId(User);
            var analysisRun = await _context.AnalysisRuns
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (analysisRun == null) return NotFound();

            var findings = JsonSerializer.Deserialize<List<Finding>>(analysisRun.ResultsJson ?? "[]");
            var fileName = $"Reporte_Analisis_{id}_{analysisRun.AnalysisTimestamp:yyyyMMdd}.pdf";
            var logoPath = Path.Combine(_hostEnvironment.WebRootPath, "img", "Bit-solucion-logo-menu.png");

            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Header().Element(header => ComposeHeader(header, analysisRun, logoPath));
                    page.Content().Element(content => ComposeContent(content, findings));
                    page.Footer().AlignCenter().Text(x => x.CurrentPageNumber().FontSize(10));
                });
            }).GeneratePdf();

            return File(pdfBytes, "application/pdf", fileName);
        }
        
        private void ComposeHeader(IContainer container, AnalysisRun run, string logoPath)
        {
            var navyColor = "#112240";
            var lightTextColor = "#E6F1FF";
            container.Background(navyColor).Padding(10).Row(row =>
            {
                if (System.IO.File.Exists(logoPath))
                {
                    row.ConstantItem(150).Image(logoPath);
                }
                row.RelativeItem().Column(column =>
                {
                    column.Item().AlignCenter().Text($"Reporte de Análisis #{run.Id}").SemiBold().FontSize(16).FontColor(lightTextColor);
                    column.Item().AlignCenter().Text($"Fecha: {run.AnalysisTimestamp:g}").FontColor(lightTextColor);
                    column.Item().AlignCenter().Text($"Usuario: {run.User.Email}").FontColor(lightTextColor);
                });
            });
        }

        private void ComposeContent(IContainer container, List<Finding> findings)
        {
            var headerBgColor = "#0A192F";
            var headerFontColor = "#E6F1FF";
            var rowColor1 = "#FFFFFF";
            var rowColor2 = "#F2F2F2";
            container.PaddingVertical(20).Column(column =>
            {
                column.Item().PaddingBottom(10).Text("Resultados Detallados").SemiBold().FontSize(14);
                
                if (findings == null || !findings.Any())
                {
                    column.Item().Text("No se encontraron problemas en este análisis.");
                    return;
                }

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(3);
                        columns.ConstantColumn(40);
                        columns.ConstantColumn(40);
                        columns.RelativeColumn(5);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(headerBgColor).Padding(5).Text("Archivo").FontColor(headerFontColor);
                        header.Cell().Background(headerBgColor).Padding(5).Text("Tipo de Problema").FontColor(headerFontColor);
                        header.Cell().Background(headerBgColor).Padding(5).Text("Celda").FontColor(headerFontColor);
                        header.Cell().Background(headerBgColor).Padding(5).Text("Línea").FontColor(headerFontColor);
                        header.Cell().Background(headerBgColor).Padding(5).Text("Contenido").FontColor(headerFontColor);
                    });

                    uint index = 0;
                    foreach (var item in findings)
                    {
                        var bgColor = index % 2 == 0 ? rowColor1 : rowColor2;
                        
                        table.Cell().Background(bgColor).Padding(5).Text(item.FileName);
                        table.Cell().Background(bgColor).Padding(5).Text(item.FindingType);
                        table.Cell().Background(bgColor).Padding(5).Text(item.CellNumber?.ToString() ?? "N/A").AlignCenter();
                        table.Cell().Background(bgColor).Padding(5).Text(item.LineNumber?.ToString() ?? "N/A").AlignCenter();
                        table.Cell().Background(bgColor).Padding(5).Text(item.Content);
                        index++;
                    }
                });
            });
        }
    }
}