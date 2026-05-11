using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotebookValidator.Web.Services;
using System;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using NotebookValidator.Web.Data;

namespace NotebookValidator.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class RegexSandboxController : Controller
    {
        private readonly AuditService _auditService;
        private readonly UserManager<ApplicationUser> _userManager;

        public RegexSandboxController(AuditService auditService, UserManager<ApplicationUser> userManager)
        {
            _auditService = auditService;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> TestRegex(string regex, string testCode)
        {
            if (string.IsNullOrEmpty(regex) || string.IsNullOrEmpty(testCode))
            {
                return Json(new { success = false, message = "Regex y código de prueba son obligatorios." });
            }

            try
            {
                var matches = Regex.Matches(testCode, regex, RegexOptions.Multiline | RegexOptions.IgnoreCase);
                var results = new System.Collections.Generic.List<object>();

                foreach (Match match in matches)
                {
                    results.Add(new
                    {
                        index = match.Index,
                        length = match.Length,
                        value = match.Value
                    });
                }

                return Json(new { success = true, count = results.Count, matches = results });
            }
            catch (ArgumentException ex)
            {
                // AUDITORÍA DE ERROR TÉCNICO (Regex mal formado)
                var userId = _userManager.GetUserId(User);
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                var details = new
                {
                    Error = ex.Message,
                    IntentoRegex = regex,
                    Modulo = "Sandbox de Reglas"
                };

                await _auditService.LogActionAsync(userId, "Sandbox: Regex Inválido", JsonSerializer.Serialize(details), ip);

                return Json(new { success = false, message = "Error de sintaxis en el Regex: " + ex.Message });
            }
        }
    }
}
