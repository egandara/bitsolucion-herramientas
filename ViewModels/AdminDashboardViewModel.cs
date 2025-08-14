using NotebookValidator.Web.Models;
using System;
using System.Collections.Generic;

namespace NotebookValidator.Web.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalAnalyses { get; set; }
        public int TotalProblemsFound { get; set; }
        public string MostActiveUser { get; set; } = "N/A";
        public List<AnalysisRun> RecentAnalyses { get; set; } = new();
        public Dictionary<string, int> ProblemTypeCounts { get; set; } = new(); // Esta es la propiedad que faltaba
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}