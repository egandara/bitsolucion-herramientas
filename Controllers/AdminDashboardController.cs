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

            if (startDate.HasValue)
            {
                query = query.Where(r => r.AnalysisTimestamp >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                query = query.Where(r => r.AnalysisTimestamp < endDate.Value.AddDays(1));
            }

            var filteredAnalyses = await query.ToListAsync();
            var allUsers = await _context.Users.ToListAsync();

            var allFindings = filteredAnalyses
                .SelectMany(run => JsonSerializer.Deserialize<List<Finding>>(run.ResultsJson) ?? new List<Finding>())
                .ToList();

            var problemTypeCounts = allFindings
                .GroupBy(f => f.FindingType)
                .ToDictionary(g => g.Key, g => g.Count());
            
            var analysesPerUser = filteredAnalyses
                .Where(r => r.User != null)
                .GroupBy(r => r.User.Email)
                .ToDictionary(g => g.Key, g => g.Count());

            var mostActiveUser = analysesPerUser.Any()
                ? analysesPerUser.OrderByDescending(kvp => kvp.Value).First().Key
                : "N/A";

            var viewModel = new AdminDashboardViewModel
            {
                TotalUsers = allUsers.Count,
                TotalAnalyses = filteredAnalyses.Count,
                TotalProblemsFound = filteredAnalyses.Sum(r => r.TotalProblemsFound),
                MostActiveUser = mostActiveUser,
                RecentAnalyses = filteredAnalyses.OrderByDescending(r => r.AnalysisTimestamp).Take(10).ToList(),
                ProblemTypeCounts = problemTypeCounts,
                StartDate = startDate,
                EndDate = endDate
            };

            return View(viewModel);
        }
    }
}