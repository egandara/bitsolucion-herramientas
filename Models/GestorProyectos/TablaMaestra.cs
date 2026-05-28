using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NotebookValidator.Web.Models; // <-- IMPORTANTE PARA ENCONTRAR 'Cliente'

namespace NotebookValidator.Web.Models.GestorProyectos
{
    public class TablaMaestra
    {
        [Key]
        public int Id { get; set; }

        // El cliente dueño de la tabla (ej. Banco BCI)
        public int? ClienteId { get; set; }
        [ForeignKey(nameof(ClienteId))]
        public Cliente? Cliente { get; set; }

        [Required]
        [MaxLength(255)]
        public string NombreTabla { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Descripcion { get; set; }

        // Visionando el futuro: Aquí guardaremos el esquema de columnas más adelante
        public string? MetadataColumnasJson { get; set; }
    }
}
