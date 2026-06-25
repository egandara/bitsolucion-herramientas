using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NotebookValidator.Web.Models.GestorProyectos
{
    public class Feriado
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateTime Fecha { get; set; }

        [Required]
        [StringLength(200)]
        public string Motivo { get; set; } = string.Empty;

        // Opcional: Si en el futuro quieres desactivar un feriado sin borrarlo de la BD
        public bool Activo { get; set; } = true;
    }
}
