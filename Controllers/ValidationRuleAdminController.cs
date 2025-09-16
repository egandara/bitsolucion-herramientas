using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotebookValidator.Web.Data;
using NotebookValidator.Web.Models;
using System.Threading.Tasks;

namespace NotebookValidator.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ValidationRuleAdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ValidationRuleAdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ValidationRuleAdmin
        public async Task<IActionResult> Index()
        {
            return View(await _context.ValidationRules.ToListAsync());
        }

        // GET: ValidationRuleAdmin/Create
        public IActionResult Create()
        {
            // Pre-llenar con valores por defecto
            var model = new ValidationRule
            {
                IsEnabled = true,
                Severity = "Warning"
            };
            return View(model);
        }

        // POST: ValidationRuleAdmin/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("RuleName,DetailsMessage,RegexPattern,Severity,IsEnabled")] ValidationRule validationRule)
        {
            if (ModelState.IsValid)
            {
                _context.Add(validationRule);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(validationRule);
        }

        // GET: ValidationRuleAdmin/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var validationRule = await _context.ValidationRules.FindAsync(id);
            if (validationRule == null) return NotFound();
            return View(validationRule);
        }

        // POST: ValidationRuleAdmin/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,RuleName,DetailsMessage,RegexPattern,Severity,IsEnabled")] ValidationRule validationRule)
        {
            if (id != validationRule.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(validationRule);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.ValidationRules.Any(e => e.Id == validationRule.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(validationRule);
        }

        // GET: ValidationRuleAdmin/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var validationRule = await _context.ValidationRules
                .FirstOrDefaultAsync(m => m.Id == id);
            if (validationRule == null) return NotFound();
            return View(validationRule);
        }

        // POST: ValidationRuleAdmin/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var validationRule = await _context.ValidationRules.FindAsync(id);
            _context.ValidationRules.Remove(validationRule);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}