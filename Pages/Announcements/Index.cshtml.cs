using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScholarSync.Data;
using ScholarSync.Models;
using ScholarSync.Security;
using System.ComponentModel.DataAnnotations;

namespace ScholarSync.Pages.Announcements;

public class IndexModel : PageModel
{
    private static readonly string[] Categories = { "General", "Exam", "Fees", "Attendance", "Holiday", "Emergency" };
    private static readonly string[] Priorities = { "Normal", "Important", "Urgent" };
    private static readonly string[] Audiences = { "Everyone", "Teachers", "Administrators" };
    private readonly SchoolDbContext _context;
    private readonly AuditService _auditService;

    public IndexModel(SchoolDbContext context, AuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public IList<SchoolAnnouncement> Announcements { get; private set; } = new List<SchoolAnnouncement>();
    public ISet<int> AcknowledgedIds { get; private set; } = new HashSet<int>();
    public IReadOnlyList<string> AvailableCategories => Categories;
    public IReadOnlyList<string> AvailablePriorities => Priorities;
    public IReadOnlyList<string> AvailableAudiences => Audiences;

    [BindProperty]
    public AnnouncementInput Input { get; set; } = new();

    public async Task OnGetAsync() => await LoadAsync();

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (!User.IsInRole("Admin")) return Forbid();
        if (!Categories.Contains(Input.Category, StringComparer.OrdinalIgnoreCase)) ModelState.AddModelError("Input.Category", "Select a valid category.");
        if (!Priorities.Contains(Input.Priority, StringComparer.OrdinalIgnoreCase)) ModelState.AddModelError("Input.Priority", "Select a valid priority.");
        if (!Audiences.Contains(Input.Audience, StringComparer.OrdinalIgnoreCase)) ModelState.AddModelError("Input.Audience", "Select a valid audience.");
        if (Input.ExpiresAt.HasValue && Input.ExpiresAt.Value < DateTime.Now) ModelState.AddModelError("Input.ExpiresAt", "Expiry must be in the future.");
        if (!ModelState.IsValid) { await LoadAsync(); return Page(); }

        var announcement = new SchoolAnnouncement
        {
            Title = Input.Title.Trim(), Message = Input.Message.Trim(), Category = Input.Category,
            Priority = Input.Priority, Audience = Input.Audience, IsPinned = Input.IsPinned,
            ExpiresAt = Input.ExpiresAt?.ToUniversalTime(), CreatedBy = User.Identity?.Name
        };
        _context.SchoolAnnouncements.Add(announcement);
        await _context.SaveChangesAsync();
        await _auditService.LogAsync("Created", "Announcement", announcement.Id, $"Published {announcement.Priority.ToLowerInvariant()} announcement '{announcement.Title}'.");
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAcknowledgeAsync(int id)
    {
        var announcement = await _context.SchoolAnnouncements.FindAsync(id);
        if (announcement == null || !CanSee(announcement)) return NotFound();
        var username = User.Identity?.Name ?? "Unknown";
        if (!await _context.AnnouncementAcknowledgements.AnyAsync(item => item.AnnouncementId == id && item.Username == username))
        {
            _context.AnnouncementAcknowledgements.Add(new AnnouncementAcknowledgement { AnnouncementId = id, Username = username });
            await _context.SaveChangesAsync();
            await _auditService.LogAsync("Acknowledged", "Announcement", id, $"Acknowledged announcement '{announcement.Title}'.");
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        if (!User.IsInRole("Admin")) return Forbid();
        var announcement = await _context.SchoolAnnouncements.FindAsync(id);
        if (announcement != null)
        {
            _context.SchoolAnnouncements.Remove(announcement);
            await _context.SaveChangesAsync();
            await _auditService.LogAsync("Deleted", "Announcement", id, $"Removed announcement '{announcement.Title}'.");
        }
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        Announcements = await _context.SchoolAnnouncements.AsNoTracking()
            .Where(item => (!item.ExpiresAt.HasValue || item.ExpiresAt > DateTime.UtcNow) && (item.Audience == "Everyone" || (item.Audience == "Teachers" && User.IsInRole("Teacher")) || (item.Audience == "Administrators" && User.IsInRole("Admin"))))
            .OrderByDescending(item => item.IsPinned).ThenByDescending(item => item.Priority == "Urgent").ThenByDescending(item => item.CreatedAt)
            .ToListAsync();
        var username = User.Identity?.Name;
        if (!string.IsNullOrWhiteSpace(username))
            AcknowledgedIds = (await _context.AnnouncementAcknowledgements.Where(item => item.Username == username && Announcements.Select(announcement => announcement.Id).Contains(item.AnnouncementId)).Select(item => item.AnnouncementId).ToListAsync()).ToHashSet();
    }

    private bool CanSee(SchoolAnnouncement item) => item.Audience == "Everyone" || (item.Audience == "Teachers" && User.IsInRole("Teacher")) || (item.Audience == "Administrators" && User.IsInRole("Admin"));

    public class AnnouncementInput
    {
        [Required, StringLength(150)] public string Title { get; set; } = string.Empty;
        [Required, StringLength(2000)] public string Message { get; set; } = string.Empty;
        public string Category { get; set; } = "General";
        public string Priority { get; set; } = "Normal";
        public string Audience { get; set; } = "Everyone";
        public bool IsPinned { get; set; }
        [DataType(DataType.DateTime)] public DateTime? ExpiresAt { get; set; }
    }
}
