using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotebookValidator.Web.Data;
using System.Linq;
using System.Threading.Tasks;

namespace NotebookValidator.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AuditController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuditController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Esta acción recibe el email y muestra los logs de ese usuario
        public async Task<IActionResult> Index(string userEmail)
        {
            ViewBag.UserFilter = userEmail;
            var logs = await _context.AuditLogs
                .Where(l => string.IsNullOrEmpty(userEmail) || l.Details.Contains(userEmail) || l.UserId == userEmail)
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();

            return View(logs);
        }
    }
}
