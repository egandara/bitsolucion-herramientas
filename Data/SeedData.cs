// EN: Data/SeedData.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace NotebookValidator.Web.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // Crear roles si no existen
            string[] roleNames = { "Admin", "User", "ValidatorUser", "TempCleanerUser", "ParameterValidatorUser", "GeminiDocUser" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Crear usuario Admin si no existe
            var adminEmail = "esteban.gandara@bitsolucion.cl"; // <-- CAMBIA ESTE EMAIL
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    AnalysisQuota = 99999 // Cuota inicial para el admin
                };
                // ¡¡CAMBIA ESTA CONTRASEÑA POR UNA SEGURA!!
                var result = await userManager.CreateAsync(adminUser, "ReyEst3b@n2025"); 

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
        }
    }
}