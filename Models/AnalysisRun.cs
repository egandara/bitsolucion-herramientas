using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NotebookValidator.Web.Data;

namespace NotebookValidator.Web.Models
{
    public class AnalysisRun
    {
        [Key]
        public int Id { get; set; }
        public DateTime AnalysisTimestamp { get; set; }
        [Required]
        public string UserId { get; set; } = string.Empty; // <-- Añadir = string.Empty;
        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; } = null!; // <-- Añadir = null!;
        public int TotalFilesAnalyzed { get; set; }
        public int TotalProblemsFound { get; set; }
        public string ResultsJson { get; set; } = string.Empty; // <-- Añadir = string.Empty;
    }
}