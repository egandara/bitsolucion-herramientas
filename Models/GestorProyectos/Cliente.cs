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
        public TimeSpan HoraEntrada { get; set; } = new TimeSpan(9, 0, 0);       // Default 09:00 AM
        public TimeSpan HoraSalidaNormal { get; set; } = new TimeSpan(18, 0, 0); // Default 18:00 PM
        public TimeSpan HoraSalidaViernes { get; set; } = new TimeSpan(16, 0, 0);// Default 16:00 PM

        // Relación: Un cliente puede tener muchos proyectos
        public ICollection<Proyecto> Proyectos { get; set; } = new List<Proyecto>();
    }
}
