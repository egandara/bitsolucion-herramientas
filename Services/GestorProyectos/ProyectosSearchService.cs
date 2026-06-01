using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotebookValidator.Web.Data;
using NotebookValidator.Web.ViewModels.GestorProyectos;

namespace NotebookValidator.Web.Services.GestorProyectos
{
    public class ProyectosSearchService
    {
        private readonly ApplicationDbContext _context;

        public ProyectosSearchService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<SearchResultDto>> GlobalSearchAsync(string query, IUrlHelper urlHelper)
        {
            var resultados = new List<SearchResultDto>();
            string q = query.ToLower();

            // Proyectos
            var proyectos = await _context.Proyectos
                .Include(p => p.Cliente)
                .Where(p => p.Nombre.ToLower().Contains(q) ||
                            (p.Cliente != null && p.Cliente.Nombre.ToLower().Contains(q)) ||
                            p.Descripcion.ToLower().Contains(q))
                .Take(5)
                .ToListAsync();

            foreach (var p in proyectos)
            {
                resultados.Add(new SearchResultDto
                {
                    Categoria = "Proyectos",
                    Titulo = p.Nombre,
                    Descripcion = p.Cliente?.Nombre ?? "Proyecto Interno",
                    Url = urlHelper.Action("Details", "Proyectos", new { id = p.Id }) ?? "#",
                    Icono = "bi-briefcase-fill text-primary"
                });
            }

            // Catálogo de linaje
            var tablas = await _context.TablasProyecto
                .Include(t => t.TablaMaestra)
                .Include(t => t.Proyecto)
                .Where(t => (t.TablaMaestra != null && t.TablaMaestra.NombreTabla.ToLower().Contains(q)) ||
                            (t.TablaMaestra != null && t.TablaMaestra.Descripcion != null && t.TablaMaestra.Descripcion.ToLower().Contains(q)))
                .Take(5)
                .ToListAsync();

            foreach (var t in tablas)
            {
                resultados.Add(new SearchResultDto
                {
                    Categoria = "Catálogo de Linaje",
                    Titulo = t.TablaMaestra?.NombreTabla ?? "Tabla Desconocida",
                    Descripcion = $"Proyecto: {t.Proyecto?.Nombre ?? "Desconocido"}",
                    Url = (urlHelper.Action("Details", "Proyectos", new { id = t.ProyectoId }) ?? "#") + "#linaje",
                    Icono = "bi-table text-success"
                });
            }

            // Archivos en workspaces indexados
            var proyectosConCodigo = await _context.Proyectos
                .Where(p => p.ArchivosIndexados != null && p.ArchivosIndexados.ToLower().Contains(q))
                .Take(5)
                .ToListAsync();

            foreach (var p in proyectosConCodigo)
            {
                var archivos = p.ArchivosIndexados?.Split(';') ?? Array.Empty<string>();
                foreach (var f in archivos.Where(f => f.ToLower().Contains(q)).Take(2))
                {
                    resultados.Add(new SearchResultDto
                    {
                        Categoria = "Código y Notebooks",
                        Titulo = f,
                        Descripcion = $"En Workspace de: {p.Nombre ?? "Desconocido"}",
                        Url = (urlHelper.Action("Details", "Proyectos", new { id = p.Id }) ?? "#") + "#calidad",
                        Icono = "bi-file-earmark-code text-info"
                    });
                }
            }

            // Bitácora y comentarios
            var comentarios = await _context.ComentariosProyecto
                .Include(c => c.Proyecto)
                .Where(c => c.Texto.ToLower().Contains(q) ||
                            (c.Usuario != null && c.Usuario.ToLower().Contains(q)))
                .Take(5)
                .ToListAsync();

            foreach (var c in comentarios)
            {
                string txt = string.IsNullOrEmpty(c.Texto) ? "" : c.Texto;
                string usr = string.IsNullOrEmpty(c.Usuario) ? "Desconocido" : c.Usuario;

                resultados.Add(new SearchResultDto
                {
                    Categoria = "Bitácora y Alertas",
                    Titulo = txt.Length > 45 ? txt.Substring(0, 45) + "..." : txt,
                    Descripcion = $"@{usr} en {c.Proyecto?.Nombre ?? "Desconocido"}",
                    Url = urlHelper.Action("Details", "Proyectos", new { id = c.ProyectoId }) ?? "#",
                    Icono = c.Tipo == "Recordatorio" ? "bi-clock-history text-warning"
                          : (c.Tipo == "Advertencia" ? "bi-exclamation-triangle text-danger"
                          : "bi-chat-left-text text-secondary")
                });
            }

            return resultados;
        }
    }
}
