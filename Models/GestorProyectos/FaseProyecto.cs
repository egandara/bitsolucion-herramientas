using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NotebookValidator.Web.Models.GestorProyectos
{
    public class FaseProyecto
    {
        [Key]
        public int Id { get; set; }

        public int ProyectoId { get; set; }
        [ForeignKey(nameof(ProyectoId))]
        public Proyecto Proyecto { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string NombreFase { get; set; } = string.Empty; // Ej: "1_Diseño", "2_Desarrollo"

        [MaxLength(50)]
        public string EstadoFase { get; set; } = "Pendiente"; // Pendiente, En Progreso, Completado

        public int Orden { get; set; } // Para mostrarlas ordenadas en la UI (1, 2, 3...)

        [MaxLength(255)]
        public string? DriveSubFolderId { get; set; }

        // ==========================================
        // NUEVAS COLUMNAS DE AUDITORÍA Y TRAZABILIDAD
        // ==========================================
        public DateTime? FechaActualizacion { get; set; }

        [MaxLength(100)]
        public string? UsuarioActualizacion { get; set; }
    }
}
