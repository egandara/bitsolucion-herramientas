// Archivo: Models/AllowedParameter.cs
using System.ComponentModel.DataAnnotations;

namespace NotebookValidator.Web.Models
{
    public class AllowedParameter
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Nombre del Widget/Variable")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Valor por Defecto")]
        public string? DefaultValue { get; set; }

        [Display(Name = "Etiqueta / Título Visual")]
        public string? Label { get; set; }

        [Display(Name = "Categoría")]
        public string? Category { get; set; } // Ej: "Widget" o "Entorno"
    }
}
