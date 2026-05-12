using System.Collections.Generic;

namespace NotebookValidator.Web.ViewModels
{
    public class UserAdminViewModel
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public int AnalysisQuota { get; set; }
        public List<string> Roles { get; set; }
        public int TotalAnalyses { get; set; }

        // NUEVOS CAMPOS
        public DateTime RegistrationDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public bool IsActive { get; set; }
    }
}
