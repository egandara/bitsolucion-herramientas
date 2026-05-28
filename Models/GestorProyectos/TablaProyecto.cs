using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NotebookValidator.Web.Models.GestorProyectos
{
    public class TablaProyecto
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProyectoId { get; set; }
        [ForeignKey(nameof(ProyectoId))]
        public Proyecto? Proyecto { get; set; }

        // Vinculación al Catálogo Maestro
        [Required]
        public int TablaMaestraId { get; set; }
        [ForeignKey(nameof(TablaMaestraId))]
        public TablaMaestra? TablaMaestra { get; set; }

        [Required]
        [MaxLength(50)]
        public string TipoTabla { get; set; } = "Origen"; // "Origen" o "Salida"

        // NUEVO CAMPO: Ruta Delta opcional
        [MaxLength(500)]
        public string? RutaUbicacion { get; set; }
    }
}
