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

        public IActionResult Index() => View();
        public IActionResult Validador() => View();

        public async Task<IActionResult> History()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var runs = await _context.AnalysisRuns
                .Where(r => r.UserId == user.Id)
                .OrderByDescending(r => r.AnalysisTimestamp)
                .ToListAsync();

            dynamic viewModel = new System.Dynamic.ExpandoObject();
            viewModel.History = runs;
            viewModel.TotalFilesAnalyzed = runs.Sum(r => r.TotalFilesAnalyzed);
            viewModel.TotalProblemsFound = runs.Sum(r => r.TotalProblemsFound);
            viewModel.AverageProblemsPerFile = runs.Any()
                ? (double)runs.Sum(r => r.TotalProblemsFound) / runs.Sum(r => r.TotalFilesAnalyzed)
                : 0;

            return View("~/Views/History/Index.cshtml", viewModel);
        }

        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var run = await _context.AnalysisRuns
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == user.Id);

            if (run == null) return NotFound();
            var findings = JsonSerializer.Deserialize<List<Finding>>(run.ResultsJson);
            ViewBag.AnalysisRun = run;
            return View("~/Views/History/Details.cshtml", findings);
        }

        public async Task<IActionResult> Stats()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            var runs = await _context.AnalysisRuns.Where(r => r.UserId == user.Id).ToListAsync();
            return View(runs);
        }

        [HttpPost]
        public async Task<IActionResult> Validate(IFormFileCollection files)
        {
            if (files == null || files.Count == 0)
                return Json(new { summary = new Dictionary<string, object>(), findings = new List<Finding>(), hasResults = false });

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var fileStreams = files.Select(f => (f.OpenReadStream(), f.FileName)).ToList();
            var (allFindings, processedCount, fileVariables) = await _validatorService.ProcessFilesAsync(fileStreams);

            if (user.AnalysisQuota < processedCount)
            {
                return Json(new { summary = new Dictionary<string, object>(), findings = new List<Finding>(), hasResults = true, quotaError = "Créditos insuficientes." });
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

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var auditDetails = new { Modulo = "Validador", Accion = "Análisis", Hallazgos = allFindings.Count, RunId = run.Id };
            await _auditService.LogActionAsync(user.Id, "VALIDACIÓN: ANÁLISIS EJECUTADO", JsonSerializer.Serialize(auditDetails), ip, run.Id.ToString());

            var summaryData = allFindings
                .GroupBy(f => new { f.FindingType, f.Severity })
                .ToDictionary(g => g.Key.FindingType, g => new { Count = g.Count(), Severity = g.Key.Severity });

            HttpContext.Session.SetString("ValidationResults", run.ResultsJson);
            HttpContext.Session.SetInt32("LastRunId", run.Id);

            return Json(new { summary = summaryData, findings = allFindings, hasResults = allFindings.Any(), fileVariables = fileVariables });
        }

        // --- ACCIÓN SMART FIX ---
        [HttpPost]
        public async Task<IActionResult> GenerateCorrected(IFormFileCollection files, string typesToCleanJson)
        {
            if (files == null || files.Count == 0) return BadRequest();
            var typesToClean = JsonSerializer.Deserialize<List<string>>(typesToCleanJson) ?? new List<string>();

            var fileStreams = files.Select(f => (f.OpenReadStream(), f.FileName)).ToList();
            var zipBytes = await _validatorService.GenerateCorrectedFilesZipAsync(fileStreams, typesToClean);

            var user = await _userManager.GetUserAsync(User);
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var details = new { Archivos = files.Select(f => f.FileName).ToList(), Limpieza = typesToClean };
            await _auditService.LogActionAsync(user.Id, "VALIDACIÓN: SMART FIX DESCARGADO", JsonSerializer.Serialize(details), ip);

            return File(zipBytes, "application/zip", $"BitSolucion_Corregido_{DateTime.Now:yyyyMMdd}.zip");
        }

        public async Task<IActionResult> ExportToExcel(int? analysisId)
        {
            var json = HttpContext.Session.GetString("ValidationResults");
            if (string.IsNullOrEmpty(json)) return RedirectToAction("Validador");
            var findings = JsonSerializer.Deserialize<List<Finding>>(json);
            var runId = analysisId ?? HttpContext.Session.GetInt32("LastRunId") ?? 0;
            var run = await _context.AnalysisRuns.Include(r => r.User).FirstOrDefaultAsync(r => r.Id == runId);
            string logoPath = Path.Combine(_hostEnvironment.WebRootPath, "img", "Bit-solucion-logo-menu.png");
            byte[] excelFile = _validatorService.GenerateExcelReportBytes(findings, run, logoPath);
            return File(excelFile, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Reporte_{DateTime.Now:yyyyMMdd}.xlsx");
        }
    }
}
