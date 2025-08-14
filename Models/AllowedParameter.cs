using System.ComponentModel.DataAnnotations;

namespace NotebookValidator.Web.Models
{
    public class AllowedParameter
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Nombre del Parámetro")]
        public string Name { get; set; } = string.Empty; // <-- Añadir = string.Empty;
    }
}