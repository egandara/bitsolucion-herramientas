// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using NotebookValidator.Web.Data;

namespace NotebookValidator.Web.Areas.Identity.Pages.Account.Manage
{
    public class ChangePasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<ChangePasswordModel> _logger;

        public ChangePasswordModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<ChangePasswordModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        public class InputModel
        {
            // --- INICIO DE CAMBIOS DE TRADUCCIÓN ---
            [Required(ErrorMessage = "El campo {0} es obligatorio.")]
            [DataType(DataType.Password)]
            [Display(Name = "Contraseña actual")]
            public string OldPassword { get; set; }

            [Required(ErrorMessage = "El campo {0} es obligatorio.")]
            [StringLength(100, ErrorMessage = "La {0} debe tener al menos {2} y como máximo {1} caracteres de longitud.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Nueva contraseña")]
            public string NewPassword { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirmar nueva contraseña")]
            [Compare("NewPassword", ErrorMessage = "La nueva contraseña y la contraseña de confirmación no coinciden.")]
            public string ConfirmPassword { get; set; }
            // --- FIN DE CAMBIOS DE TRADUCCIÓN ---
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                // --- CAMBIO DE TRADUCCIÓN ---
                return NotFound($"No se pudo cargar el usuario con ID '{_userManager.GetUserId(User)}'.");
            }

            var hasPassword = await _userManager.HasPasswordAsync(user);
            if (!hasPassword)
            {
                return RedirectToPage("./SetPassword");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                // --- CAMBIO DE TRADUCCIÓN ---
                return NotFound($"No se pudo cargar el usuario con ID '{_userManager.GetUserId(User)}'.");
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(user, Input.OldPassword, Input.NewPassword);
            if (!changePasswordResult.Succeeded)
            {
                foreach (var error in changePasswordResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return Page();
            }

            await _signInManager.RefreshSignInAsync(user);

            // --- CAMBIO DE TRADUCCIÓN ---
            _logger.LogInformation("El usuario cambió su contraseña exitosamente.");
            StatusMessage = "Tu contraseña ha sido cambiada.";

            // Lógica para desmarcar el cambio de contraseña obligatorio
            user.MustChangePassword = false;
            await _userManager.UpdateAsync(user);
            return RedirectToPage();
        }
    }
}