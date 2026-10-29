using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScholarSync.Data;
using ScholarSync.Security;

namespace ScholarSync.Pages.Attendance;

public class IndexModel : PageModel
{
    private readonly SchoolDbContext _context;
    private readonly AuditService _auditService;
    private readonly SchoolAccessService _accessService;

    public IndexModel(SchoolDbContext context, AuditService auditService, SchoolAccessService accessService)
    {
        _context = context;
        _auditService = auditService;
        _accessService = accessService;
    }

    [BindProperty(SupportsGet = true)]
    public DateTime AttendanceDate { get; set; } = DateTime.Today;

    [BindProperty]
    public IList<AttendanceEntry> Entries { get; set; } = new List<AttendanceEntry>();

    public async Task OnGetAsync()
    {
        await LoadEntriesAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var date = AttendanceDate.Date;
        var studentIds = Entries.Select(entry => entry.StudentId).ToList();
        if (!_accessService.IsAdmin)
        {
            var allowedClassIds = await _accessService.GetAllowedClassIdsAsync();
            var allowedStudentIds = await _context.Students.Where(student => student.SchoolClassId.HasValue && allowedClassIds.Contains(student.SchoolClassId.Value) && studentIds.Contains(student.Id)).Select(student => student.Id).ToListAsync();
            if (allowedStudentIds.Count != studentIds.Distinct().Count())
                return Forbid();
        }
        var records = await _context.AttendanceRecords
            .Where(record => record.AttendanceDate == date && studentIds.Contains(record.StudentId))
            .ToListAsync();

        foreach (var entry in Entries)
        {
            var record = records.FirstOrDefault(existing => existing.StudentId == entry.StudentId);
            if (record == null)
            {
                _context.AttendanceRecords.Add(new Models.AttendanceRecord
                {
                    StudentId = entry.StudentId,
                    AttendanceDate = date,
                    Status = entry.Status
                });
            }
            else
            {
                record.Status = entry.Status;
            }
        }

        await _context.SaveChangesAsync();
        await _auditService.LogAsync("Saved", "Attendance", null, $"Updated attendance for {Entries.Count} students on {date:yyyy-MM-dd}.");
        return RedirectToPage(new { AttendanceDate = date.ToString("yyyy-MM-dd") });
    }

    private async Task LoadEntriesAsync()
    {
        var date = AttendanceDate.Date;
        var students = await _context.Students
            .AsNoTracking()
            .OrderBy(student => student.LastName)
            .ThenBy(student => student.FirstName)
            .ToListAsync();
        if (!_accessService.IsAdmin)
        {
            var allowedClassIds = await _accessService.GetAllowedClassIdsAsync();
            students = students.Where(student => student.SchoolClassId.HasValue && allowedClassIds.Contains(student.SchoolClassId.Value)).ToList();
        }

        var records = await _context.AttendanceRecords
            .Where(record => record.AttendanceDate == date)
            .ToDictionaryAsync(record => record.StudentId, record => record.Status);

        Entries = students.Select(student => new AttendanceEntry
        {
            StudentId = student.Id,
            StudentName = $"{student.FirstName} {student.LastName}",
            Status = records.GetValueOrDefault(student.Id, "Present")
        }).ToList();
    }

    public class AttendanceEntry
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string Status { get; set; } = "Present";
    }
}
