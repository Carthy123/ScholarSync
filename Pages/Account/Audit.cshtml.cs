using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScholarSync.Data;
using ScholarSync.Models;

namespace ScholarSync.Pages.Account;

[Authorize(Roles = "Admin")]
public class AuditModel : PageModel
{
    private readonly SchoolDbContext _context;

    public AuditModel(SchoolDbContext context) => _context = context;

    public IList<AuditLog> Logs { get; private set; } = new List<AuditLog>();

    public async Task OnGetAsync()
    {
        Logs = await _context.AuditLogs
            .AsNoTracking()
            .OrderByDescending(log => log.CreatedAt)
            .Take(200)
            .ToListAsync();
    }
}
