using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotebookValidator.Web.Data;
using NotebookValidator.Web.Models.GestorProyectos;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;

namespace NotebookValidator.Web.Controllers
{
    [Authorize]
    public class ClientesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClientesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // CLIENTES
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var clientes = await _context.Clientes
                .OrderByDescending(c => c.Activo)
                .ThenBy(c => c.Nombre)
                .ToListAsync();

            return View(clientes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string nombre)
        {
            if (!string.IsNullOrWhiteSpace(nombre))
            {
                _context.Clientes.Add(new Cliente { Nombre = nombre.Trim().ToUpper(), Activo = true });
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente != null)
            {
                cliente.Activo = !cliente.Activo;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // CATÁLOGO CENTRALIZADO DE LINAJE
        // =========================================================

        /// <summary>
        /// Vista principal del catálogo global — todas las tablas agrupadas por cliente
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Catalogo(int? clienteId, string? q)
        {
            var clientes = await _context.Clientes
                .Where(c => c.Activo)
                .OrderBy(c => c.Nombre)
                .ToListAsync();

            var query = _context.TablasMaestras
                .Include(t => t.Cliente)
                .Include(t => t.TablasProyecto)
                    .ThenInclude(tp => tp.Proyecto)
                .AsQueryable();

            if (clienteId.HasValue)
                query = query.Where(t => t.ClienteId == clienteId.Value);

            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(t => t.NombreTabla.ToLower().Contains(q.ToLower()) ||
                                         (t.Descripcion != null && t.Descripcion.ToLower().Contains(q.ToLower())));

            var tablas = await query
                .OrderBy(t => t.ClienteId)
                .ThenBy(t => t.NombreTabla)
                .ToListAsync();

            ViewBag.Clientes = clientes;
            ViewBag.ClienteId = clienteId;
            ViewBag.Q = q;
            ViewBag.TotalTablas = tablas.Count;
            ViewBag.TotalProyectos = tablas.SelectMany(t => t.TablasProyecto).Select(tp => tp.ProyectoId).Distinct().Count();

            return View(tablas);
        }

        /// <summary>
        /// Agregar tabla maestra desde el catálogo centralizado
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarTabla(int clienteId, string nombreTabla, string? descripcion)
        {
            if (string.IsNullOrWhiteSpace(nombreTabla))
                return Json(new { success = false, message = "El nombre de la tabla es obligatorio." });

            bool existe = await _context.TablasMaestras.AnyAsync(t =>
                t.ClienteId == clienteId &&
                t.NombreTabla.ToLower() == nombreTabla.Trim().ToLower());

            if (existe)
                return Json(new { success = false, message = "Esta tabla ya existe en el catálogo de este cliente." });

            var tabla = new TablaMaestra
            {
                ClienteId = clienteId,
                NombreTabla = nombreTabla.Trim(),
                Descripcion = descripcion?.Trim()
            };

            _context.TablasMaestras.Add(tabla);
            await _context.SaveChangesAsync();

            return Json(new { success = true, id = tabla.Id, nombre = tabla.NombreTabla, descripcion = tabla.Descripcion ?? "" });
        }

        /// <summary>
        /// Editar descripción de una tabla maestra
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarTabla(int id, string descripcion)
        {
            var tabla = await _context.TablasMaestras.FindAsync(id);
            if (tabla == null) return Json(new { success = false, message = "Tabla no encontrada." });

            tabla.Descripcion = descripcion?.Trim();
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        /// <summary>
        /// Eliminar tabla maestra (solo si no está en uso en ningún proyecto)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EliminarTabla(int id)
        {
            var tabla = await _context.TablasMaestras
                .Include(t => t.TablasProyecto)
                .AsSplitQuery()
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tabla == null)
                return Json(new { success = false, message = "Tabla no encontrada." });

            if (tabla.TablasProyecto.Any())
                return Json(new { success = false, message = $"Esta tabla está en uso en {tabla.TablasProyecto.Count} proyecto(s). Quítala de los proyectos antes de eliminarla." });

            _context.TablasMaestras.Remove(tabla);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        /// <summary>
        /// Buscar tablas para autocomplete (usada desde el formulario de linaje en proyectos)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> BuscarTablas(string q, int? clienteId)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
                return Json(new List<object>());

            var query = _context.TablasMaestras
                .Include(t => t.Cliente)
                .Where(t => t.NombreTabla.ToLower().Contains(q.ToLower()));

            if (clienteId.HasValue)
                query = query.Where(t => t.ClienteId == clienteId.Value);

            var resultados = await query
                .OrderBy(t => t.NombreTabla)
                .Take(10)
                .Select(t => new {
                    id = t.Id,
                    nombre = t.NombreTabla,
                    descripcion = t.Descripcion ?? "",
                    cliente = t.Cliente != null ? t.Cliente.Nombre : "Sin cliente"
                })
                .ToListAsync();

            return Json(resultados);
        }
    }
}
