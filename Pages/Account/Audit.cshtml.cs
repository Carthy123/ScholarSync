using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScholarSync.Data;
using ScholarSync.Models;
using ScholarSync.Security;

namespace ScholarSync.Pages.Account;

[Authorize(Roles = "Admin")]
public class AuditModel : PageModel
{
    private readonly SchoolDbContext _context;
    private readonly AuditService _auditService;

    public AuditModel(SchoolDbContext context, AuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public IList<AuditLog> Logs { get; private set; } = new List<AuditLog>();
    public IList<AdminUser> LockedUsers { get; private set; } = new List<AdminUser>();
    public IList<string> Actions { get; private set; } = new List<string>();
    public string Search { get; private set; } = string.Empty;
    public string SelectedAction { get; private set; } = string.Empty;

    public async Task OnGetAsync(string? search = null, string? action = null)
    {
        Search = search?.Trim() ?? string.Empty;
        SelectedAction = action?.Trim() ?? string.Empty;

        var logsQuery = _context.AuditLogs.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(Search))
        {
            logsQuery = logsQuery.Where(log =>
                log.Username.Contains(Search) ||
                log.Action.Contains(Search) ||
                log.EntityType.Contains(Search) ||
                log.Details.Contains(Search));
        }
        if (!string.IsNullOrWhiteSpace(SelectedAction))
            logsQuery = logsQuery.Where(log => log.Action == SelectedAction);

        Logs = await logsQuery
            .AsNoTracking()
            .OrderByDescending(log => log.CreatedAt)
            .Take(200)
            .ToListAsync();

        Actions = await _context.AuditLogs
            .AsNoTracking()
            .Select(log => log.Action)
            .Distinct()
            .OrderBy(actionName => actionName)
            .ToListAsync();

        LockedUsers = await _context.AdminUsers
            .AsNoTracking()
            .Where(user => user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
            .OrderBy(user => user.LockoutEnd)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostUnlockAsync(int id)
    {
        var user = await _context.AdminUsers.FindAsync(id);
        if (user != null)
        {
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            await _context.SaveChangesAsync();
            await _auditService.LogAsync("Unlocked", "AdminUser", user.Id, $"Removed the temporary lockout for '{user.Username}'.");
        }

        return RedirectToPage();
    }
}
