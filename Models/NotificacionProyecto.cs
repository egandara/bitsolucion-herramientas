using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NotebookValidator.Web.Models
{
    public class NotificacionProyecto
    {
        [Key]
        public int Id { get; set; }

        // Usuario destinatario
        [Required]
        public string UsuarioId { get; set; } = string.Empty;

        // Proyecto relacionado (nullable para notificaciones del sistema)
        public int? ProyectoId { get; set; }

        // Tipo de evento
        [Required]
        [MaxLength(50)]
        public string Tipo { get; set; } = string.Empty;
        // Valores: "ValidacionRechazada", "CodigoSubido", "Mencion",
        //          "RecordatorioVencido", "DocumentoSubido", "FaseCambiada"

        [Required]
        [MaxLength(255)]
        public string Titulo { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Descripcion { get; set; }

        // URL a la que navegar al hacer clic
        [MaxLength(500)]
        public string? Url { get; set; }

        public bool Leida { get; set; } = false;

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }
}
