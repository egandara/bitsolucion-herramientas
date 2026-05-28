using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotebookValidator.Web.Data;
using NotebookValidator.Web.Models.GestorProyectos;
using System.Threading.Tasks;
using System.Linq;

namespace NotebookValidator.Web.Controllers
{
    [Authorize] // Protegemos el mantenedor
    public class ClientesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClientesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // 1. VISTA PRINCIPAL: Lista de clientes y formulario de agregar
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

        // =========================================================
        // 2. CREAR CLIENTE RÁPIDO
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string nombre)
        {
            if (!string.IsNullOrWhiteSpace(nombre))
            {
                var nuevoCliente = new Cliente
                {
                    Nombre = nombre.Trim().ToUpper(), // Lo pasamos a mayúsculas por estándar
                    Activo = true
                };

                _context.Clientes.Add(nuevoCliente);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // 3. ACTIVAR / DESACTIVAR CLIENTE
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente != null)
            {
                cliente.Activo = !cliente.Activo; // Invierte el estado
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
