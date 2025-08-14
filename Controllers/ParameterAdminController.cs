using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotebookValidator.Web.Data;
using NotebookValidator.Web.Models;
using System.Linq;
using System.Threading.Tasks;

namespace NotebookValidator.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ParameterAdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ParameterAdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Muestra la lista de parámetros
        public async Task<IActionResult> Index()
        {
            // Se añade .ToLower() para asegurar un ordenamiento alfabético insensible a mayúsculas/minúsculas
            var sortedParameters = await _context.AllowedParameters
                .OrderBy(p => p.Name.ToLower())
                .ToListAsync();
            
            return View(sortedParameters);
        }
        
        // Procesa la creación de un nuevo parámetro
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name")] AllowedParameter allowedParameter)
        {
            if (ModelState.IsValid)
            {
                _context.Add(allowedParameter);
                await _context.SaveChangesAsync();
                TempData["ToastMessage"] = "Parámetro añadido correctamente.";
            }
            return RedirectToAction(nameof(Index));
        }

        // Procesa la eliminación de un parámetro
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var allowedParameter = await _context.AllowedParameters.FindAsync(id);
            if (allowedParameter != null)
            {
                _context.AllowedParameters.Remove(allowedParameter);
                await _context.SaveChangesAsync();
                TempData["ToastMessage"] = "Parámetro eliminado correctamente.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}