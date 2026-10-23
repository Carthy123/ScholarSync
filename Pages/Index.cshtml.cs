using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScholarSync.Data;
using ScholarSync.Models;

namespace ScholarSync.Pages;

public class IndexModel : PageModel
{
    private readonly SchoolDbContext _context;

    public IndexModel(SchoolDbContext context)
    {
        _context = context;
    }

    public int StudentCount { get; private set; }
    public int ClassCount { get; private set; }
    public int UnassignedStudentCount { get; private set; }
    public int TodayPresentCount { get; private set; }
    public int TodayAbsentCount { get; private set; }
    public int TodayLateCount { get; private set; }
    public IList<Student> RecentStudents { get; private set; } = new List<Student>();
    public IList<GradeRecord> RecentGrades { get; private set; } = new List<GradeRecord>();
    public SchoolSettings Settings { get; private set; } = new();

    public async Task OnGetAsync()
    {
        Settings = await _context.SchoolSettings.FirstOrDefaultAsync(settings => settings.Id == 1) ?? new SchoolSettings();
        StudentCount = await _context.Students.CountAsync();
        ClassCount = await _context.Classes.CountAsync();
        UnassignedStudentCount = await _context.Students.CountAsync(student => student.SchoolClassId == null);
        var today = DateTime.Today;
        TodayPresentCount = await _context.AttendanceRecords.CountAsync(record => record.AttendanceDate == today && record.Status == "Present");
        TodayAbsentCount = await _context.AttendanceRecords.CountAsync(record => record.AttendanceDate == today && record.Status == "Absent");
        TodayLateCount = await _context.AttendanceRecords.CountAsync(record => record.AttendanceDate == today && record.Status == "Late");
        RecentStudents = await _context.Students
            .Include(student => student.SchoolClass)
            .AsNoTracking()
            .OrderByDescending(student => student.Id)
            .Take(5)
            .ToListAsync();
        RecentGrades = await _context.GradeRecords
            .Include(record => record.Student)
            .AsNoTracking()
            .OrderByDescending(record => record.RecordedAt)
            .Take(5)
            .ToListAsync();
    }
}
