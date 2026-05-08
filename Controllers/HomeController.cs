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
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
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

        public HomeController(
            NotebookValidatorService validatorService,
            IWebHostEnvironment hostEnvironment,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _validatorService = validatorService;
            _hostEnvironment = hostEnvironment;
            _userManager = userManager;
            _context = context;
        }

        public IActionResult Index() => View();

        [HttpPost]
        public async Task<IActionResult> Validate(IFormFileCollection files)
        {
            if (files == null || files.Count == 0)
                return Json(new { summary = new Dictionary<string, object>(), findings = new List<Finding>(), hasResults = false });

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var fileStreams = files.Select(f => (f.OpenReadStream(), f.FileName)).ToList();
            var (allFindings, processedCount) = await _validatorService.ProcessFilesAsync(fileStreams);

            if (user.AnalysisQuota < processedCount)
            {
                return Json(new
                {
                    summary = new Dictionary<string, object> { { "Error", new { Count = 1, Severity = "Critical" } } },
                    findings = new List<Finding>(),
                    hasResults = true,
                    quotaError = "Créditos insuficientes."
                });
            }

            user.AnalysisQuota -= processedCount;
            await _userManager.UpdateAsync(user);

            var run = new AnalysisRun
            {
                UserId = user.Id,
                AnalysisTimestamp = DateTime.Now,
                TotalFilesAnalyzed = processedCount,
                TotalProblemsFound = allFindings.Count,
                ResultsJson = JsonSerializer.Serialize(allFindings)
            };
            _context.AnalysisRuns.Add(run);
            await _context.SaveChangesAsync();

            // Cálculo del resumen para la interfaz gráfica
            var summaryData = allFindings
                .GroupBy(f => new { f.FindingType, f.Severity })
                .ToDictionary(
                    g => g.Key.FindingType,
                    g => new { Count = g.Count(), Severity = g.Key.Severity }
                );

            HttpContext.Session.SetString("ValidationResults", run.ResultsJson);
            HttpContext.Session.SetInt32("LastRunId", run.Id);

            return Json(new
            {
                summary = summaryData,
                findings = allFindings,
                hasResults = allFindings.Any()
            });
        }

        public async Task<IActionResult> ExportToExcel(int? analysisId)
        {
            var json = HttpContext.Session.GetString("ValidationResults");
            if (string.IsNullOrEmpty(json)) return RedirectToAction("Index");

            var findings = JsonSerializer.Deserialize<List<Finding>>(json);
            var runId = analysisId ?? HttpContext.Session.GetInt32("LastRunId") ?? 0;
            var run = await _context.AnalysisRuns.Include(r => r.User).FirstOrDefaultAsync(r => r.Id == runId);

            string logoPath = Path.Combine(_hostEnvironment.WebRootPath, "img", "Bit-solucion-logo-menu.png");
            byte[] excelFile = _validatorService.GenerateExcelReportBytes(findings, run, logoPath);

            return File(excelFile, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Reporte_{DateTime.Now:yyyyMMdd}.xlsx");
        }
    }
}
