using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NotebookValidator.Web.Models.GestorProyectos
{
    public class ArtefactoJob
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProyectoId { get; set; }

        [ForeignKey(nameof(ProyectoId))]
        public Proyecto? Proyecto { get; set; }

        public DateTime FechaGeneracion { get; set; } = DateTime.Now;

        [MaxLength(100)]
        public string UsuarioGenerador { get; set; } = string.Empty; // Correo de quien apretó el botón

        [MaxLength(255)]
        public string NombreBundle { get; set; } = string.Empty; // Ej: Data-dbs-datosnorm-plt-migraarchnorm

        [MaxLength(500)]
        public string? ArchivoDriveUrl { get; set; } // El link al .zip respaldado en la nube
    }
}
