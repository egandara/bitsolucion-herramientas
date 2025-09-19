using System;
using System.Threading.Tasks;
using NotebookValidator.Web.Data;
using NotebookValidator.Web.Models;

namespace NotebookValidator.Web.Services
{
    public class AuditService
    {
        private readonly ApplicationDbContext _context;

        public AuditService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogActionAsync(string userId, string actionType, string details, string entityId = null)
        {
            var auditLog = new AuditLog
            {
                UserId = userId,
                ActionType = actionType,
                Timestamp = DateTime.UtcNow,
                Details = details,
                EntityId = entityId
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
    }
}