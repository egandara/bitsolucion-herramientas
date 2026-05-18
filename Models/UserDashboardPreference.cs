using System;
using System.ComponentModel.DataAnnotations;

namespace NotebookValidator.Web.Models
{
    public class UserDashboardPreference
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string UserEmail { get; set; } // Vinculado a User.Identity.Name

        [Required]
        public string LayoutJson { get; set; } // Aquí guardaremos el string JSON con el orden y tamaño

        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}
