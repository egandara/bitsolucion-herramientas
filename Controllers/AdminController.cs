using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotebookValidator.Web.Data;
using NotebookValidator.Web.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NotebookValidator.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
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
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, roles); // Quitamos todos los roles para empezar de cero

            // Añadimos los roles seleccionados en el formulario
            await _userManager.AddToRolesAsync(user, model.Roles.Where(r => r.IsSelected).Select(r => r.RoleName));

            TempData["ToastMessage"] = $"Roles de {user.Email} actualizados.";
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