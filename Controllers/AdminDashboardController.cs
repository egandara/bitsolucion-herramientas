using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotebookValidator.Web.Data;
using NotebookValidator.Web.Models;
using NotebookValidator.Web.Services;
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
            var querySummaries = _context.AnalysisSummaries.AsNoTracking().AsQueryable();

            // Filtros de fecha sobre la tabla de resumen (muy rápidos)
            if (startDate.HasValue)
                querySummaries = querySummaries.Where(s => s.AnalysisTimestamp >= startDate.Value);
            if (endDate.HasValue)
                querySummaries = querySummaries.Where(s => s.AnalysisTimestamp < endDate.Value.AddDays(1));

            // 1. ESTADÍSTICAS BÁSICAS (100% precisas y ultra rápidas)
            var totalUsers = await _context.Users.CountAsync();
            var totalAnalyses = await querySummaries.CountAsync();
            var totalProblems = await querySummaries.SumAsync(s => s.CriticalCount + s.WarningCount + s.InfoCount);

            // 2. TABLA DE ANÁLISIS RECIENTES (Seguimos usando la técnica ligera de proyección)
            var recentAnalyses = await _context.AnalysisRuns.AsNoTracking()
                .OrderByDescending(r => r.AnalysisTimestamp)
                .Take(10)
                .Select(r => new AnalysisRun
                {
                    Id = r.Id,
                    AnalysisTimestamp = r.AnalysisTimestamp,
                    TotalFilesAnalyzed = r.TotalFilesAnalyzed,
                    TotalProblemsFound = r.TotalProblemsFound,
                    User = new ApplicationUser { Email = r.User.Email }
                })
                .ToListAsync();

            // 3. TENDENCIA (Leída directamente de los resúmenes)
            var limiteTendencia = DateTime.Now.Date.AddDays(-15);
            var tendenciaDataRaw = await querySummaries
                .Where(s => s.AnalysisTimestamp >= limiteTendencia)
                .GroupBy(s => s.AnalysisTimestamp.Date)
                .Select(g => new { Fecha = g.Key, Cantidad = g.Count() })
                .OrderBy(x => x.Fecha)
                .ToListAsync();

            // 4. DISTRIBUCIÓN DE RIESGO (¡Ahora con el 100% de los datos!)
            // Sumamos los contadores pre-calculados de todos los registros filtrados
            var totalCritical = await querySummaries.SumAsync(s => s.CriticalCount);
            var totalWarning = await querySummaries.SumAsync(s => s.WarningCount);
            var totalInfo = await querySummaries.SumAsync(s => s.InfoCount);

            var problemTypeSeverities = new Dictionary<string, string> {
        { "Crítico", "Critical" }, { "Advertencia", "Warning" }, { "Informativo", "Info" }
    };
            var problemTypeCounts = new Dictionary<string, int> {
        { "Crítico", totalCritical }, { "Advertencia", totalWarning }, { "Informativo", totalInfo }
    };

            // 5. USUARIO MÁS ACTIVO (Consulta de metadata)
            var mostActiveUserId = await _context.AnalysisRuns.AsNoTracking()
                .GroupBy(r => r.UserId)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefaultAsync();

            string mostActiveUser = "N/A";
            if (mostActiveUserId != null)
            {
                mostActiveUser = await _context.Users.AsNoTracking()
                    .Where(u => u.Id == mostActiveUserId)
                    .Select(u => u.Email)
                    .FirstOrDefaultAsync() ?? "N/A";
            }

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

        [HttpGet]
        public async Task<IActionResult> MigrateHistory([FromServices] NotebookValidatorService validatorService)
        {
            // 1. Obtener solo los IDs que faltan (esto es muy rápido y ligero)
            var existingIds = await _context.AnalysisSummaries.Select(s => s.AnalysisRunId).ToListAsync();
            var idsToMigrate = await _context.AnalysisRuns
                .Where(r => !existingIds.Contains(r.Id))
                .Select(r => r.Id) // Solo traemos el ID, ignoramos el JSON pesado por ahora
                .ToListAsync();

            int successCount = 0;
            int errorCount = 0;

            // 2. Procesamos cada registro de forma individual
            foreach (var runId in idsToMigrate)
            {
                try
                {
                    // Traemos el registro completo (incluyendo el JSON) solo para este ID
                    var run = await _context.AnalysisRuns
                        .AsNoTracking()
                        .FirstOrDefaultAsync(r => r.Id == runId);

                    if (run == null || string.IsNullOrEmpty(run.ResultsJson)) continue;

                    // Deserializamos y creamos el resumen
                    var findings = JsonSerializer.Deserialize<List<Finding>>(run.ResultsJson);
                    if (findings != null)
                    {
                        var summary = validatorService.CreateSummary(findings, run.Id, run.AnalysisTimestamp);

                        _context.AnalysisSummaries.Add(summary);

                        // 3. Guardamos inmediatamente después de cada registro
                        // Esto evita que la transacción de SQL sea demasiado grande
                        await _context.SaveChangesAsync();
                        successCount++;
                    }
                }
                catch (Exception)
                {
                    errorCount++;
                    // Si un registro falla (ej. JSON corrupto), continuamos con el siguiente
                }
            }

            return Content($"Migración finalizada. Procesados con éxito: {successCount}, Errores: {errorCount}.");
        }
    }
}
