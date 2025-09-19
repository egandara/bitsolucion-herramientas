using System;
using System.ComponentModel.DataAnnotations;
using NotebookValidator.Web.Data; // Necesario para ApplicationUser

namespace NotebookValidator.Web.Models
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        public virtual ApplicationUser User { get; set; }

        [Required]
        public string ActionType { get; set; } // Ej: "UserRolesChanged", "QuotaUpdated"

        [Required]
        public DateTime Timestamp { get; set; }

        public string EntityId { get; set; } // El ID del objeto afectado (ej: el ID del usuario modificado)

        [Required]
        public string Details { get; set; } // Mensaje descriptivo del evento
    }
}