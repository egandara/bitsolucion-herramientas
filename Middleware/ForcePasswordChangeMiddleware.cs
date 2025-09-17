using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using NotebookValidator.Web.Data;
using System.Threading.Tasks;

namespace NotebookValidator.Web.Middleware
{
    public class ForcePasswordChangeMiddleware
    {
        private readonly RequestDelegate _next;

        public ForcePasswordChangeMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, UserManager<ApplicationUser> userManager)
        {
            if (context.User.Identity.IsAuthenticated)
            {
                var user = await userManager.GetUserAsync(context.User);
                if (user != null && user.MustChangePassword)
                {
                    // Evitar un bucle infinito de redirecciones
                    var managePath = "/Identity/Account/Manage/ChangePassword";
                    var logoutPath = "/Identity/Account/Logout";

                    if (!context.Request.Path.Equals(managePath, StringComparison.OrdinalIgnoreCase) &&
                        !context.Request.Path.Equals(logoutPath, StringComparison.OrdinalIgnoreCase))
                    {
                        context.Response.Redirect(managePath);
                        return; // Importante para detener el procesamiento
                    }
                }
            }

            await _next(context);
        }
    }
}