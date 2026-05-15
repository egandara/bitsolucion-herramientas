using Microsoft.AspNetCore.Identity;
using System;

namespace NotebookValidator.Web.Data
{
    public class ApplicationUser : IdentityUser
    {
        public int AnalysisQuota { get; set; }
        public bool MustChangePassword { get; set; }

        // CAMPOS NULABLES (Importante el ?)
        public DateTime? RegistrationDate { get; set; } = DateTime.Now;
        public DateTime? LastLoginDate { get; set; }
        public DateTime? LastPasswordChangedDate { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
