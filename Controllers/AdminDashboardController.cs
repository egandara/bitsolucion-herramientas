using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotebookValidator.Web.Data;
using NotebookValidator.Web.Models;
using NotebookValidator.Web.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace NotebookValidator.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminDashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate)
        {
            IQueryable<AnalysisRun> query = _context.AnalysisRuns.Include(r => r.User);

            if (startDate.HasValue) query = query.Where(r => r.AnalysisTimestamp >= startDate.Value);
            if (endDate.HasValue) query = query.Where(r => r.AnalysisTimestamp < endDate.Value.AddDays(1));

            var filteredAnalyses = await query.ToListAsync();
            var allUsers = await _context.Users.ToListAsync();

            // 1. Procesar Hallazgos para la Dona
            var allFindings = filteredAnalyses
                .SelectMany(run => JsonSerializer.Deserialize<List<Finding>>(run.ResultsJson) ?? new List<Finding>())
                .ToList();

            var problemTypeCounts = allFindings.GroupBy(f => f.FindingType).ToDictionary(g => g.Key, g => g.Count());
            var problemTypeSeverities = allFindings.GroupBy(f => f.FindingType).ToDictionary(g => g.Key, g => g.FirstOrDefault()?.Severity ?? "Info");

            // 2. Calcular Tendencia (Últimos 15 días)
            var limiteTendencia = DateTime.Now.Date.AddDays(-14);
            var tendenciaData = filteredAnalyses
                .Where(r => r.AnalysisTimestamp >= limiteTendencia)
                .GroupBy(r => r.AnalysisTimestamp.Date)
                .Select(g => new { Fecha = g.Key, Cantidad = g.Count() })
                .OrderBy(g => g.Fecha)
                .ToList();

            // 3. Usuario más activo
            var mostActiveUser = filteredAnalyses
                .Where(r => r.User != null)
                .GroupBy(r => r.User.Email)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault() ?? "N/A";

            var viewModel = new AdminDashboardViewModel
            {
                TotalUsers = allUsers.Count,
                TotalAnalyses = filteredAnalyses.Count,
                TotalProblemsFound = filteredAnalyses.Sum(r => r.TotalProblemsFound),
                MostActiveUser = mostActiveUser,
                RecentAnalyses = filteredAnalyses.OrderByDescending(r => r.AnalysisTimestamp).Take(8).ToList(),
                ProblemTypeCounts = problemTypeCounts,
                ProblemTypeSeverities = problemTypeSeverities,
                TrendLabels = tendenciaData.Select(d => d.Fecha.ToString("dd MMM")).ToList(),
                TrendData = tendenciaData.Select(d => d.Cantidad).ToList(),
                StartDate = startDate,
                EndDate = endDate
            };

            return View(viewModel);
        }
    }
}
