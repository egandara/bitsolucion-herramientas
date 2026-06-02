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

        // Navegación hacia las subfases
        public ICollection<SubFaseProyecto> SubFases { get; set; } = new List<SubFaseProyecto>();

        // Propiedad calculada opcional para el progreso de la Fase basada en sus Subfases
        [NotMapped]
        public int PorcentajeCompletado
        {
            get
            {
                if (SubFases == null || !SubFases.Any()) return EstadoFase == "Completado" ? 100 : 0;
                int total = SubFases.Count;
                int completadas = SubFases.Count(s => s.Estado == "Completado");
                return (int)Math.Round(((double)completadas / total) * 100);
            }
        }
    }

}
