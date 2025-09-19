using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotebookValidator.Web.Data;
using NotebookValidator.Web.Services;
using NotebookValidator.Web.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static SkiaSharp.HarfBuzz.SKShaper;

namespace NotebookValidator.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AuditService _auditService;

        public AdminController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, Services.AuditService auditService)
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
            if (id == null) return NotFound();
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.AnalysisQuota = analysisQuota;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["ToastMessage"] = $"La cuota de {user.Email} se ha actualizado correctamente.";
                TempData["ToastType"] = "success";
                return RedirectToAction("Index");
            }

            TempData["ToastMessage"] = "Ocurrió un error al actualizar la cuota.";
            TempData["ToastType"] = "error";
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(user);
        }

        public async Task<IActionResult> ManageRoles(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var viewModel = new ManageUserRolesViewModel
            {
                UserId = user.Id,
                UserEmail = user.Email,
                Roles = new List<UserRoleViewModel>()
            };

            // CORRECCIÓN: Ahora incluimos todos los roles en la lista
            foreach (var role in _roleManager.Roles)
            {
                var userRoleViewModel = new UserRoleViewModel
                {
                    RoleName = role.Name,
                    IsSelected = await _userManager.IsInRoleAsync(user, role.Name)
                };
                viewModel.Roles.Add(userRoleViewModel);
            }
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageRoles(ManageUserRolesViewModel model)
        {
            var adminUser = await _userManager.GetUserAsync(User); // Obtenemos el admin que realiza la acción
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null) return NotFound();

            var oldRoles = await _userManager.GetRolesAsync(user);
            var selectedRoles = model.Roles.Where(r => r.IsSelected).Select(r => r.RoleName);

            var result = await _userManager.RemoveFromRolesAsync(user, oldRoles);
            result = await _userManager.AddToRolesAsync(user, selectedRoles);

            if (user == null)
            {
                TempData["ToastrMessage"] = "El usuario no fue encontrado.";
                TempData["ToastrType"] = "error";
                return NotFound();
            }

            // Primero, quitamos todos los roles actuales del usuario.
            var currentRoles = await _userManager.GetRolesAsync(user);
            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);

            if (!removeResult.Succeeded)
            {
                TempData["ToastrMessage"] = "Error al remover los roles actuales del usuario.";
                TempData["ToastrType"] = "error";
                return RedirectToAction("Index");
            }

            // Luego, añadimos solo los roles que fueron seleccionados en el formulario.
            var addResult = await _userManager.AddToRolesAsync(user, selectedRoles);

            if (addResult.Succeeded)
            {
                var details = $"Roles de '{user.Email}' cambiados de [{string.Join(", ", oldRoles)}] a [{string.Join(", ", selectedRoles)}].";
                await _auditService.LogActionAsync(adminUser.Id, "UserRolesChanged", details, user.Id);

                TempData["ToastrMessage"] = $"Los roles para el usuario {user.Email} han sido actualizados correctamente.";
                TempData["ToastrType"] = "success";
            }
            else
            {
                TempData["ToastrMessage"] = "Ocurrió un error al actualizar los roles del usuario.";
                TempData["ToastrType"] = "error";
            }

            return RedirectToAction("Index");
        }

        // GET: Muestra la confirmación para reiniciar la contraseña
        [HttpGet]
        public async Task<IActionResult> ResetPassword(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        // POST: Ejecuta el reinicio de la contraseña
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPasswordConfirmed(string Id) // <-- Nombre corregido
        {
            var user = await _userManager.FindByIdAsync(Id);
            if (user == null)
            {
                return NotFound();
            }

            // Generar una contraseña temporal segura
            var temporaryPassword = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Substring(0, 10) + "aA1!";

            // Reiniciar la contraseña del usuario
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, temporaryPassword);

            if (result.Succeeded)
            {
                // Marcar al usuario para que deba cambiar la contraseña en el próximo inicio de sesión
                user.MustChangePassword = true;
                await _userManager.UpdateAsync(user);

                // Mostrar la contraseña temporal al administrador
                ViewBag.TemporaryPassword = temporaryPassword;
                ViewBag.UserName = user.UserName;
                return View("ResetPasswordSuccess");
            }

            // Si hay un error, mostrarlo
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View("ResetPassword", user); // <-- Corregido para devolver el modelo correcto
        }
    }
}