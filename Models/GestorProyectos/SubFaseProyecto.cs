using NotebookValidator.Web.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NotebookValidator.Web.Models.GestorProyectos
{
    public class SubFaseProyecto
    {
        [Key]
        public int Id { get; set; }

        // Relación con la Fase Padre
        public int FaseProyectoId { get; set; }
        [ForeignKey(nameof(FaseProyectoId))]
        public FaseProyecto Fase { get; set; } = null!;

        [Required]
        [MaxLength(150)]
        public string Nombre { get; set; } = string.Empty; // Ej: "Validador C11"

        [MaxLength(50)]
        public string Estado { get; set; } = "Pendiente"; // Pendiente, En Progreso, Completado

        // Asignación individual (Relación directa con AspNetUsers)
        public string? ResponsableId { get; set; }
        [ForeignKey(nameof(ResponsableId))]
        public ApplicationUser? Responsable { get; set; }

        public string? DriveSubFolderId { get; set; } // Para adjuntar documentos específicos

        // =========================================================
        // NUEVAS COLUMNAS: FECHAS DE LA SUBFASE
        // =========================================================
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFinEstimada { get; set; }

        // Relación para comentarios específicos de la subfase
        public ICollection<ComentarioProyecto> Comentarios { get; set; } = new List<ComentarioProyecto>();
    }
}
