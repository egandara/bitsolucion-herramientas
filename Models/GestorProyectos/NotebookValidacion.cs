using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NotebookValidator.Web.Models.GestorProyectos
{
    public class NotebookValidacion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProyectoId { get; set; }

        [ForeignKey(nameof(ProyectoId))]
        public Proyecto? Proyecto { get; set; }

        [Required]
        [MaxLength(255)]
        public string NombreArchivo { get; set; } = string.Empty;

        [Required]
        public DateTime FechaValidacion { get; set; }

        [Required]
        [MaxLength(50)]
        public string Usuario { get; set; } = string.Empty; // Quién lo validó

        [Required]
        public bool PasoValidacion { get; set; } // true = Aprobado, false = Rechazado

        public int Score { get; set; } // Ejemplo: 85% de cumplimiento de reglas

        public string? DetalleErrores { get; set; } // JSON o texto con las celdas que fallaron
    }
}
