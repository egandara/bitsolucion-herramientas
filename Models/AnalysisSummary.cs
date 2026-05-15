using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NotebookValidator.Web.Models
{
    public class AnalysisSummary
    {
        [Key]
        public int Id { get; set; }

        // Relación con el análisis original
        public int AnalysisRunId { get; set; }
        [ForeignKey("AnalysisRunId")]
        public AnalysisRun AnalysisRun { get; set; } = null!;

        public DateTime AnalysisTimestamp { get; set; }

        // Conteos pre-calculados (Lo que leerá el Dashboard)
        public int CriticalCount { get; set; }
        public int WarningCount { get; set; }
        public int InfoCount { get; set; }

        // Resumen de tipos (Ej: {"ImportError": 5, "SQL": 2})
        public string FindingTypesSummaryJson { get; set; } = "{}";
    }
}
