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
            // 1. Iniciamos consulta base con AsNoTracking para eficiencia
            var query = _context.AnalysisRuns.AsNoTracking().AsQueryable();

            // Filtros de fecha (opcionales)
            if (startDate.HasValue) query = query.Where(r => r.AnalysisTimestamp >= startDate.Value);
            if (endDate.HasValue) query = query.Where(r => r.AnalysisTimestamp < endDate.Value.AddDays(1));

            // 2. ESTADÍSTICAS BÁSICAS (Rápido: Solo números)
            var totalUsers = await _context.Users.CountAsync();
            var totalAnalyses = await query.CountAsync();
            var totalProblems = await query.SumAsync(r => r.TotalProblemsFound);

            // 3. ANÁLISIS RECIENTES (Ligero: Metadata + Email del Usuario)
            // Proyectamos a un nuevo objeto para IGNORAR la columna ResultsJson pesada
            var recentAnalyses = await query
                .OrderByDescending(r => r.AnalysisTimestamp)
                .Take(10)
                .Select(r => new AnalysisRun
                {
                    Id = r.Id,
                    AnalysisTimestamp = r.AnalysisTimestamp,
                    TotalFilesAnalyzed = r.TotalFilesAnalyzed,
                    TotalProblemsFound = r.TotalProblemsFound,
                    UserId = r.UserId,
                    // Traemos solo el email para evitar ciclos infinitos
                    User = new ApplicationUser { Email = r.User.Email }
                })
                .ToListAsync();

            // 4. USUARIO MÁS ACTIVO (Directo en SQL)
            var mostActiveUserId = await query
                .Where(r => r.UserId != null)
                .GroupBy(r => r.UserId)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefaultAsync();

            string mostActiveUser = "N/A";
            if (!string.IsNullOrEmpty(mostActiveUserId))
            {
                mostActiveUser = await _context.Users
                    .Where(u => u.Id == mostActiveUserId)
                    .Select(u => u.Email)
                    .FirstOrDefaultAsync() ?? "N/A";
            }

            // 5. TENDENCIA (Agrupación nativa por fecha para Chart.js)
            var limiteTendencia = DateTime.Now.Date.AddDays(-15);
            var tendenciaDataRaw = await query
                .Where(r => r.AnalysisTimestamp >= limiteTendencia)
                .GroupBy(r => r.AnalysisTimestamp.Date)
                .Select(g => new { Fecha = g.Key, Cantidad = g.Count() })
                .OrderBy(x => x.Fecha)
                .ToListAsync();

            // 6. DISTRIBUCIÓN DE RIESGO (Limitado: Solo procesamos los últimos 5 JSONs)
            // Esto evita que el servidor se congele procesando miles de hallazgos
            var limitedJsonData = await query
                .OrderByDescending(r => r.AnalysisTimestamp)
                .Take(5)
                .Select(r => r.ResultsJson)
                .ToListAsync();

            var allFindings = new List<Finding>();
            foreach (var json in limitedJsonData.Where(j => !string.IsNullOrEmpty(j)))
            {
                try
                {
                    var findings = JsonSerializer.Deserialize<List<Finding>>(json);
                    if (findings != null) allFindings.AddRange(findings);
                }
                catch { /* Ignorar si hay error en el JSON */ }
            }

            var problemTypeCounts = allFindings.GroupBy(f => f.FindingType).ToDictionary(g => g.Key, g => g.Count());
            var problemTypeSeverities = allFindings.GroupBy(f => f.FindingType).ToDictionary(g => g.Key, g => g.FirstOrDefault()?.Severity ?? "Warning");

            // 7. CONSTRUCCIÓN DEL MODELO FINAL
            var viewModel = new AdminDashboardViewModel
            {
                TotalUsers = totalUsers,
                TotalAnalyses = totalAnalyses,
                TotalProblemsFound = totalProblems,
                MostActiveUser = mostActiveUser,
                RecentAnalyses = recentAnalyses,
                ProblemTypeCounts = problemTypeCounts,
                ProblemTypeSeverities = problemTypeSeverities,
                TrendLabels = tendenciaDataRaw.Select(d => d.Fecha.ToString("dd MMM")).ToList(),
                TrendData = tendenciaDataRaw.Select(d => d.Cantidad).ToList(),
                StartDate = startDate,
                EndDate = endDate
            };

            return View(viewModel);
        }
    }
}
