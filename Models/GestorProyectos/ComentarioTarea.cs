using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NotebookValidator.Web.Models.GestorProyectos
{
    public class ComentarioTarea
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TareaProyectoId { get; set; }

        [ForeignKey(nameof(TareaProyectoId))]
        public virtual TareaProyecto? Tarea { get; set; }

        [Required]
        [StringLength(100)]
        public string UsuarioAlias { get; set; } = string.Empty;

        [Required]
        public string Texto { get; set; } = string.Empty;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}
