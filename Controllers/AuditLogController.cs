using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotebookValidator.Web.Data;
using System.Linq;
using System.Threading.Tasks;

namespace NotebookValidator.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AuditLogController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuditLogController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var auditLogs = await _context.AuditLogs
                .Include(a => a.User) // Incluimos los datos del usuario que realizó la acción
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();

            return View(auditLogs);
        }
    }
}