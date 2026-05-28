using NotebookValidator.Web.Data;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NotebookValidator.Web.Models.GestorProyectos
{
    public class ProyectoUsuario
    {
        public int ProyectoId { get; set; }
        [ForeignKey(nameof(ProyectoId))]
        public Proyecto Proyecto { get; set; } = null!;

        // Fíjate que es string, para calzar con el nvarchar(450) de tu tabla AspNetUsers
        [Required]
        [MaxLength(450)]
        public string UsuarioId { get; set; } = string.Empty;

        // NOTA: Reemplaza "ApplicationUser" por el nombre de tu clase custom de Identity si se llama distinto
        [ForeignKey(nameof(UsuarioId))]
        public ApplicationUser Usuario { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string RolEnProyecto { get; set; } = "Developer"; // Admin, Developer, Viewer

        public DateTime FechaAsignacion { get; set; } = DateTime.Now;
    }
}
