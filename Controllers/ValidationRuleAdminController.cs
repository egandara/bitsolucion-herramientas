using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotebookValidator.Web.Data;
using NotebookValidator.Web.Models;
using NotebookValidator.Web.Services;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace NotebookValidator.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ValidationRuleAdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditService _auditService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ValidationRuleAdminController(ApplicationDbContext context, AuditService auditService, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _auditService = auditService;
            _userManager = userManager;
        }

        // GET: ValidationRuleAdmin
        public async Task<IActionResult> Index()
        {
            return View(await _context.ValidationRules.ToListAsync());
        }

        // GET: ValidationRuleAdmin/Create
        public IActionResult Create()
        {
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

                // AUDITORÍA DE CREACIÓN
                var adminId = _userManager.GetUserId(User);
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                var detailsObj = new
                {
                    Modulo = "Motor de Reglas",
                    Accion = "Creación de Nueva Regla",
                    NombreRegla = validationRule.RuleName,
                    Configuracion = new
                    {
                        Regex = validationRule.RegexPattern,
                        Severidad = validationRule.Severity,
                        Mensaje = validationRule.DetailsMessage
                    }
                };

                await _auditService.LogActionAsync(
                    adminId,
                    "Creación de Regla",
                    JsonSerializer.Serialize(detailsObj),
                    ip,
                    validationRule.Id.ToString());

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
                    // Capturamos el estado anterior SIN seguimiento para la auditoría
                    var oldRule = await _context.ValidationRules.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);

                    _context.Update(validationRule);
                    await _context.SaveChangesAsync();

                    // AUDITORÍA DE EDICIÓN
                    var adminId = _userManager.GetUserId(User);
                    var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                    var detailsObj = new
                    {
                        Modulo = "Motor de Reglas",
                        NombreRegla = validationRule.RuleName,
                        Cambios = new
                        {
                            Regex = oldRule.RegexPattern != validationRule.RegexPattern ? new { Antes = oldRule.RegexPattern, Nuevo = validationRule.RegexPattern } : null,
                            Severidad = oldRule.Severity != validationRule.Severity ? new { Antes = oldRule.Severity, Nuevo = validationRule.Severity } : null,
                            Estado = oldRule.IsEnabled != validationRule.IsEnabled ? new { Antes = oldRule.IsEnabled, Nuevo = validationRule.IsEnabled } : null
                        }
                    };

                    await _auditService.LogActionAsync(
                        adminId,
                        "Actualización de Regla",
                        JsonSerializer.Serialize(detailsObj),
                        ip,
                        id.ToString());
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
            var validationRule = await _context.ValidationRules.FirstOrDefaultAsync(m => m.Id == id);
            if (validationRule == null) return NotFound();
            return View(validationRule);
        }

        // POST: ValidationRuleAdmin/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var validationRule = await _context.ValidationRules.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);

            if (validationRule != null)
            {
                var adminId = _userManager.GetUserId(User);
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                var detailsObj = new
                {
                    Modulo = "Motor de Reglas",
                    Accion = "Eliminación de Regla",
                    NombreRegla = validationRule.RuleName,
                    UltimoRegex = validationRule.RegexPattern
                };

                await _auditService.LogActionAsync(
                    adminId,
                    "Eliminación de Regla",
                    JsonSerializer.Serialize(detailsObj),
                    ip,
                    id.ToString());

                // Eliminación física
                var ruleToDelete = new ValidationRule { Id = id };
                _context.Entry(ruleToDelete).State = EntityState.Deleted;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
