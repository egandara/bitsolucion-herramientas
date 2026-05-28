using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NotebookValidator.Web.Models;
using NotebookValidator.Web.Models.GestorProyectos; // Asegúrate de tener este using

namespace NotebookValidator.Web.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<AnalysisRun> AnalysisRuns { get; set; }
        public DbSet<AnalysisSummary> AnalysisSummaries { get; set; }
        public DbSet<AllowedParameter> AllowedParameters { get; set; }
        public DbSet<ValidationRule> ValidationRules { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<UserDashboardPreference> UserDashboardPreferences { get; set; }

        // =========================================================
        // NUEVAS TABLAS: GESTOR DE PROYECTOS
        // =========================================================
        public DbSet<Proyecto> Proyectos { get; set; }
        public DbSet<FaseProyecto> FasesProyecto { get; set; }
        public DbSet<ProyectoUsuario> ProyectosUsuarios { get; set; }
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<NotebookValidacion> NotebookValidaciones { get; set; }
        public DbSet<TablaProyecto> TablasProyecto { get; set; }
        public DbSet<TablaMaestra> TablasMaestras { get; set; }
        public DbSet<ArtefactoJob> ArtefactosJob { get; set; }
        // LA NUEVA TABLA QUE ACABAMOS DE CREAR:
        public DbSet<ComentarioProyecto> ComentariosProyecto { get; set; }

        // =========================================================
        // CONFIGURACIÓN AVANZADA DE MODELOS (Fluent API)
        // =========================================================
        protected override void OnModelCreating(ModelBuilder builder)
        {
            // ¡MUUUY IMPORTANTE! Esto configura las tablas de Identity (AspNetUsers, etc.)
            base.OnModelCreating(builder);

            // Configurar la llave primaria compuesta para la tabla puente ProyectoUsuario
            builder.Entity<ProyectoUsuario>()
                .HasKey(pu => new { pu.ProyectoId, pu.UsuarioId });

            // Configurar el borrado en cascada (Si borras un proyecto, se borran sus asignaciones)
            builder.Entity<ProyectoUsuario>()
                .HasOne(pu => pu.Proyecto)
                .WithMany(p => p.UsuariosAsignados)
                .HasForeignKey(pu => pu.ProyectoId)
                .OnDelete(DeleteBehavior.Cascade);

            // (Opcional) Borrado en cascada para las fases del proyecto
            builder.Entity<FaseProyecto>()
                .HasOne(f => f.Proyecto)
                .WithMany(p => p.Fases)
                .HasForeignKey(f => f.ProyectoId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
