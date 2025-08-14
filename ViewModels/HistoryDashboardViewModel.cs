// EN: ViewModels/HistoryDashboardViewModel.cs
using NotebookValidator.Web.Models;
using System.Collections.Generic;

namespace NotebookValidator.Web.ViewModels
{
    public class HistoryDashboardViewModel
    {
        // Para las tarjetas de KPIs
        public int TotalAnalyses { get; set; }
        public int TotalFilesAnalyzed { get; set; }
        public int TotalProblemsFound { get; set; }
        public string? MostCommonProblem { get; set; }

        // Para el gráfico
        public Dictionary<string, int> ProblemTypeCounts { get; set; } = new();

        // Para la tabla de historial que ya existía
        public List<AnalysisRun> AnalysisRuns { get; set; } = new();
    }
}