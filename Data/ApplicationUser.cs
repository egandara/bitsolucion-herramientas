// EN: Data/ApplicationUser.cs
using Microsoft.AspNetCore.Identity;

namespace NotebookValidator.Web.Data
{
    public class ApplicationUser : IdentityUser
    {
        public int AnalysisQuota { get; set; }
        public bool MustChangePassword { get; set; }
    }
}