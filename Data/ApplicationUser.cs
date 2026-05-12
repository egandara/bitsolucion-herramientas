using Microsoft.AspNetCore.Identity;
using System;

namespace NotebookValidator.Web.Data
{
    public class ApplicationUser : IdentityUser
    {
        public int AnalysisQuota { get; set; }
        public bool MustChangePassword { get; set; }

        // NUEVOS CAMPOS PARA AUDITORÍA
        public DateTime RegistrationDate { get; set; } = DateTime.Now;
        public DateTime? LastLoginDate { get; set; }
        public bool IsActive { get; set; } = true; // Para desactivar cuentas sin borrarlas
    }
}
