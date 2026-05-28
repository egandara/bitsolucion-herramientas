using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NotebookValidator.Web.Models.GestorProyectos
{
    public class Cliente
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        public bool Activo { get; set; } = true;

        // Relación: Un cliente puede tener muchos proyectos
        public ICollection<Proyecto> Proyectos { get; set; } = new List<Proyecto>();
    }
}
