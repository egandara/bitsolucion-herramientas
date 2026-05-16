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

        public async Task<IActionResult> Index()
        {
            var sortedParameters = await _context.AllowedParameters
                .OrderBy(p => p.Name.ToLower())
                .ToListAsync();

            return View(sortedParameters);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,DefaultValue,Label,Category")] AllowedParameter allowedParameter)
        {
            if (ModelState.IsValid)
            {
                _context.Add(allowedParameter);
                await _context.SaveChangesAsync();
                TempData["ToastMessage"] = "Parámetro añadido correctamente.";
            }
            return RedirectToAction(nameof(Index));
        }

        // ACCIÓN DE EDICIÓN
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,DefaultValue,Label,Category")] AllowedParameter allowedParameter)
        {
            if (id != allowedParameter.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(allowedParameter);
                    await _context.SaveChangesAsync();
                    TempData["ToastMessage"] = "Parámetro actualizado correctamente.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.AllowedParameters.Any(e => e.Id == allowedParameter.Id)) return NotFound();
                    else throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }

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

        // ACCIÓN DE INSERCIÓN RÁPIDA (AJAX/JSON) DESDE EL VALIDADOR
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateJson(AllowedParameter allowedParameter)
        {
            if (ModelState.IsValid)
            {
                // Validar que no exista duplicado preventivamente
                var exists = await _context.AllowedParameters.AnyAsync(p => p.Name.ToLower() == allowedParameter.Name.ToLower());
                if (exists)
                {
                    return Json(new { success = false, message = "El parámetro ya existe en el maestro oficial." });
                }

                _context.Add(allowedParameter);
                await _context.SaveChangesAsync();
                return Json(new
                {
                    success = true,
                    name = allowedParameter.Name,
                    label = allowedParameter.Label,
                    category = allowedParameter.Category
                });
            }
            return Json(new { success = false, message = "Los datos ingresados no cumplen con las reglas de validación." });
        }
    }
}
