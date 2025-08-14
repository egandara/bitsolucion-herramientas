using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NotebookValidator.Web.Models;

namespace NotebookValidator.Web.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<AnalysisRun> AnalysisRuns { get; set; }
        public DbSet<AllowedParameter> AllowedParameters { get; set; }
    }
}