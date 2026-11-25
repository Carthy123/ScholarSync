using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScholarSync.Data;
using ScholarSync.Models;
using ScholarSync.Security;

namespace ScholarSync.Pages.Operations;

public class IndexModel : PageModel
{
    private readonly SchoolDbContext _context;
    private readonly SchoolAccessService _accessService;

    public IndexModel(SchoolDbContext context, SchoolAccessService accessService)
    {
        _context = context;
        _accessService = accessService;
    }

    public int StudentCount { get; private set; }
    public int PresentCount { get; private set; }
    public int AbsentCount { get; private set; }
    public int LateCount { get; private set; }
    public int UnmarkedCount => Math.Max(StudentCount - PresentCount - AbsentCount - LateCount, 0);
    public IList<SchoolEvent> UpcomingEvents { get; private set; } = new List<SchoolEvent>();
    public IList<SchoolAnnouncement> Announcements { get; private set; } = new List<SchoolAnnouncement>();

    public async Task OnGetAsync()
    {
        var allowedClassIds = await _accessService.GetAllowedClassIdsAsync();
        var studentsQuery = _context.Students.AsNoTracking();
        if (!_accessService.IsAdmin)
            studentsQuery = studentsQuery.Where(student => student.SchoolClassId.HasValue && allowedClassIds.Contains(student.SchoolClassId.Value));

        var studentIds = await studentsQuery.Select(student => student.Id).ToListAsync();
        StudentCount = studentIds.Count;

        var today = DateTime.Today;
        var todayRecords = await _context.AttendanceRecords
            .AsNoTracking()
            .Where(record => record.AttendanceDate == today && studentIds.Contains(record.StudentId))
            .Select(record => record.Status)
            .ToListAsync();
        PresentCount = todayRecords.Count(status => status == "Present");
        AbsentCount = todayRecords.Count(status => status == "Absent");
        LateCount = todayRecords.Count(status => status == "Late");

        UpcomingEvents = await _context.SchoolEvents
            .AsNoTracking()
            .Where(schoolEvent => schoolEvent.EndDate >= today)
            .OrderBy(schoolEvent => schoolEvent.StartDate)
            .ThenBy(schoolEvent => schoolEvent.Title)
            .Take(6)
            .ToListAsync();

        var now = DateTime.UtcNow;
        var announcementsQuery = _context.SchoolAnnouncements
            .AsNoTracking()
            .Where(announcement => !announcement.ExpiresAt.HasValue || announcement.ExpiresAt > now);
        if (!_accessService.IsAdmin)
        {
            var audience = User.IsInRole("Teacher") ? "Teachers" : "Everyone";
            announcementsQuery = announcementsQuery.Where(announcement => announcement.Audience == "Everyone" || announcement.Audience == audience);
        }
        Announcements = await announcementsQuery
            .OrderByDescending(announcement => announcement.IsPinned)
            .ThenByDescending(announcement => announcement.CreatedAt)
            .Take(6)
            .ToListAsync();
    }
}
