using NotebookValidator.Web.Data;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NotebookValidator.Web.Models.GestorProyectos
{
    public class TareaProyecto
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SubFaseProyectoId { get; set; }
        [ForeignKey(nameof(SubFaseProyectoId))]
        public virtual SubFaseProyecto? SubFase { get; set; }

        [Required]
        [StringLength(200)]
        public string Nombre { get; set; } = string.Empty;

        public string? Descripcion { get; set; }

        [StringLength(50)]
        public string Estado { get; set; } = "Pendiente"; // Pendiente, En Progreso, Bloqueada, Terminada

        public string? UsuarioAsignadoId { get; set; }
        [ForeignKey(nameof(UsuarioAsignadoId))]

        // ==========================================
        // DEPENDENCIA DE TAREAS (Finish-to-Start)
        // ==========================================
        public int? TareaPredecesoraId { get; set; }

        [ForeignKey(nameof(TareaPredecesoraId))]
        public virtual TareaProyecto? TareaPredecesora { get; set; }

        public virtual ApplicationUser? UsuarioAsignado { get; set; }

        // ==========================================
        // VISTA 1: LO PLANIFICADO (Por el Admin)
        // ==========================================
        public DateTime? FechaInicioPlanificada { get; set; }
        public DateTime? FechaFinPlanificada { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal HorasEstimadas { get; set; } = 0;

        // ==========================================
        // VISTA 2: LO EXPERIMENTAL / REAL (Telemetría)
        // ==========================================
        public DateTime? FechaInicioReal { get; set; }
        public DateTime? FechaFinReal { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal HorasRealesDeducidas { get; set; } = 0;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}
