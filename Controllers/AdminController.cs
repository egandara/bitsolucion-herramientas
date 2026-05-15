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
using System.Text.Json;
using System.Threading.Tasks;

namespace NotebookValidator.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AuditService _auditService;
        private readonly ApplicationDbContext _context;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            AuditService auditService,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _auditService = auditService;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var userViewModels = new List<UserAdminViewModel>();

            foreach (var user in users)
            {
                userViewModels.Add(new UserAdminViewModel
                {
                    Id = user.Id,
                    Email = user.Email,
                    AnalysisQuota = user.AnalysisQuota,
                    Roles = (await _userManager.GetRolesAsync(user)).ToList(),
                    TotalAnalyses = await _context.AnalysisRuns.CountAsync(r => r.UserId == user.Id),

                    // ASIGNACIÓN CORREGIDA:
                    // Usamos ?? DateTime.MinValue para convertir DateTime? a DateTime de forma segura
                    RegistrationDate = user.RegistrationDate ?? DateTime.MinValue,
                    LastLoginDate = user.LastLoginDate,
                    IsActive = user.IsActive
                });
            }

            return View(userViewModels);
        }

        // NUEVA ACCIÓN: Activar/Desactivar Usuario
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);

            // Auditoría del cambio de estado
            var adminId = _userManager.GetUserId(User);
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _auditService.LogActionAsync(adminId, "Cambio de Estado Usuario",
                $"Usuario {user.Email} marcado como {(user.IsActive ? "Activo" : "Inactivo")}", ip, user.Id);

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
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
                var adminId = _userManager.GetUserId(User);
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                var detailsObj = new { Modulo = "Admin", Usuario = user.Email, Anterior = oldQuota, Nuevo = analysisQuota };
                await _auditService.LogActionAsync(adminId, "Actualización de Cuota", JsonSerializer.Serialize(detailsObj), ip, user.Id);
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        [HttpGet]
        public async Task<IActionResult> ManageRoles(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            var roles = await _roleManager.Roles.ToListAsync();
            var userRoles = await _userManager.GetRolesAsync(user);
            return View(new ManageUserRolesViewModel
            {
                UserId = id,
                UserEmail = user.Email,
                Roles = roles.Select(r => new UserRoleViewModel { RoleName = r.Name, IsSelected = userRoles.Contains(r.Name) }).ToList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageRoles(ManageUserRolesViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null) return NotFound();
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (model.Roles == null) return View(model);
            var selectedRoles = model.Roles.Where(x => x.IsSelected).Select(y => y.RoleName).ToList();
            var rolesToAdd = selectedRoles.Except(currentRoles).ToList();
            var rolesToRemove = currentRoles.Except(selectedRoles).ToList();

            if (rolesToRemove.Any()) await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            if (rolesToAdd.Any()) await _userManager.AddToRolesAsync(user, rolesToAdd);

            var adminId = _userManager.GetUserId(User);
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _auditService.LogActionAsync(adminId, "Gestión de Roles", JsonSerializer.Serialize(new { Usuario = user.Email, Roles = selectedRoles }), ip, user.Id);
            return RedirectToAction(nameof(Index));
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
                ViewBag.TemporaryPassword = temporaryPassword;
                ViewBag.UserName = user.UserName;
                return View("ResetPasswordSuccess");
            }
            return View("ResetPassword", user);
        }
    }
}
