using System.ComponentModel.DataAnnotations;

namespace NotebookValidator.Web.Models
{
    public class ValidationRule
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Nombre de la Regla")]
        public string RuleName { get; set; }

        [Required]
        [Display(Name = "Mensaje de Detalle")]
        public string DetailsMessage { get; set; }

        [Required]
        [Display(Name = "Patrón Regex")]
        public string RegexPattern { get; set; }

        [Required]
        [Display(Name = "Severidad")]
        public string Severity { get; set; } // "Critical", "Warning", "Info"

        [Display(Name = "Habilitada")]
        public bool IsEnabled { get; set; }
    }
}