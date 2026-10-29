using ScholarSync.Data;
using ScholarSync.Models;

namespace ScholarSync.Security;

public class AuditService
{
    private readonly SchoolDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(SchoolDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(string action, string entityType, int? entityId = null, string details = "")
    {
        _context.AuditLogs.Add(new AuditLog
        {
            Username = _httpContextAccessor.HttpContext?.User.Identity?.Name ?? "System",
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details
        });
        await _context.SaveChangesAsync();
    }
}
