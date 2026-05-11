using System;
using System.ComponentModel.DataAnnotations;
using NotebookValidator.Web.Data;

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
        public string Action { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }

        // El '?' permite que el campo sea opcional en la base de datos
        public string? EntityId { get; set; }

        public string? IpAddress { get; set; }

        [Required]
        public string Details { get; set; }
    }
}
