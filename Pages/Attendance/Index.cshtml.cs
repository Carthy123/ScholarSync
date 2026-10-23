using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScholarSync.Data;

namespace ScholarSync.Pages.Attendance;

public class IndexModel : PageModel
{
    private readonly SchoolDbContext _context;

    public IndexModel(SchoolDbContext context)
    {
        _context = context;
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
