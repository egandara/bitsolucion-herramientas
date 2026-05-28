using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NotebookValidator.Web.Models.GestorProyectos
{
    public class ComentarioProyecto
    {
        [Key]
        public int Id { get; set; }

        public int ProyectoId { get; set; }
        [ForeignKey(nameof(ProyectoId))]
        public Proyecto Proyecto { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string Usuario { get; set; } = string.Empty;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        [Required]
        public string Texto { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Tipo { get; set; } = "Nota"; // Nota, Advertencia, Recordatorio

        public DateTime? FechaVencimiento { get; set; } // Solo se usa si el Tipo es "Recordatorio"

        public bool Resuelto { get; set; } = false; // Para que el equipo pueda "apagar" la alerta del recordatorio
    }
}
