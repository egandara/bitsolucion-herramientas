using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotebookValidator.Web.Data;
using NotebookValidator.Web.Services;
using NotebookValidator.Web.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json; // Necesario para serializar los detalles del log
using System.Threading.Tasks;

namespace NotebookValidator.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AuditService _auditService;

        public AdminController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, AuditService auditService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _auditService = auditService;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            return View(users);
        }

        public async Task<IActionResult> EditQuota(string id)
        {
            if (id == null) return NotFound();
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditQuota(string id, int analysisQuota)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var oldQuota = user.AnalysisQuota;
            user.AnalysisQuota = analysisQuota;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                // AUDITORÍA
                var adminId = _userManager.GetUserId(User);
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                var detailsObj = new
                {
                    Modulo = "Administración de Usuarios",
                    UsuarioAfectado = user.Email,
                    Campo = "Cuota de Análisis",
                    ValorAnterior = oldQuota,
                    ValorNuevo = analysisQuota,
                    FechaAccion = DateTime.Now.ToString("G")
                };

                await _auditService.LogActionAsync(
                    adminId,
                    "Actualización de Cuota",
                    JsonSerializer.Serialize(detailsObj),
                    ip,
                    user.Id);

                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        public async Task<IActionResult> ManageRoles(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var roles = await _roleManager.Roles.ToListAsync();
            var userRoles = await _userManager.GetRolesAsync(user);

            // CORRECCIÓN: Usando las propiedades exactas de tu ManageUserRolesViewModel
            var model = new ManageUserRolesViewModel
            {
                UserId = userId,
                UserEmail = user.Email,
                Roles = roles.Select(r => new UserRoleViewModel
                {
                    RoleName = r.Name,
                    IsSelected = userRoles.Contains(r.Name)
                }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageRoles(ManageUserRolesViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var result = await _userManager.RemoveFromRolesAsync(user, roles);

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "No se pudieron eliminar los roles existentes");
                return View(model);
            }

            // CORRECCIÓN: Usando model.Roles en lugar de UserRoles
            var selectedRoles = model.Roles.Where(x => x.IsSelected).Select(y => y.RoleName).ToList();
            result = await _userManager.AddToRolesAsync(user, selectedRoles);

            if (result.Succeeded)
            {
                // AUDITORÍA
                var adminId = _userManager.GetUserId(User);
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                var detailsObj = new
                {
                    Modulo = "Seguridad / Roles",
                    UsuarioAfectado = user.Email,
                    RolesAnteriores = roles,
                    RolesNuevos = selectedRoles,
                    CambioExitoso = true
                };

                await _auditService.LogActionAsync(
                    adminId,
                    "Gestión de Roles",
                    JsonSerializer.Serialize(detailsObj),
                    ip,
                    user.Id);

                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError("", "No se pudieron agregar los nuevos roles");
            return View(model);
        }

        public async Task<IActionResult> ResetPassword(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPasswordConfirmed(string Id)
        {
            var user = await _userManager.FindByIdAsync(Id);
            if (user == null) return NotFound();

            var temporaryPassword = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Substring(0, 10) + "aA1!";

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, temporaryPassword);

            if (result.Succeeded)
            {
                user.MustChangePassword = true;
                await _userManager.UpdateAsync(user);

                // AUDITORÍA
                var adminId = _userManager.GetUserId(User);
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                var detailsObj = new
                {
                    Modulo = "Seguridad / Cuentas",
                    UsuarioAfectado = user.Email,
                    Accion = "Reinicio Forzado de Contraseña",
                    RequiereCambioEnLogin = true
                };

                await _auditService.LogActionAsync(
                    adminId,
                    "Reset de Password",
                    JsonSerializer.Serialize(detailsObj),
                    ip,
                    user.Id);

                ViewBag.TemporaryPassword = temporaryPassword;
                ViewBag.UserName = user.UserName;
                return View("ResetPasswordSuccess");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View("ResetPassword", user);
        }
    }
}
