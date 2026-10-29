using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScholarSync.Data;
using ScholarSync.Models;
using ScholarSync.Security;

namespace ScholarSync.Pages.Reports;

public class IndexModel : PageModel
{
    private readonly SchoolDbContext _context;
    private readonly SchoolAccessService _accessService;

    public IndexModel(SchoolDbContext context, SchoolAccessService accessService)
    {
        _context = context;
        _accessService = accessService;
    }

    public IList<Student> Students { get; private set; } = new List<Student>();
    public Student? SelectedStudent { get; private set; }
    public IList<GradeRecord> Grades { get; private set; } = new List<GradeRecord>();
    public IList<AttendanceRecord> Attendance { get; private set; } = new List<AttendanceRecord>();
    public SchoolSettings Settings { get; private set; } = new();
    public string CrestInitials => string.Concat(Settings.SchoolName
        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .Take(2)
        .Select(word => char.ToUpperInvariant(word[0])));
    public decimal AverageScore => Grades.Count == 0 ? 0 : Math.Round(Grades.Average(record => record.Score), 2);
    public int PresentCount => Attendance.Count(record => record.Status == "Present");
    public int AbsentCount => Attendance.Count(record => record.Status == "Absent");
    public int LateCount => Attendance.Count(record => record.Status == "Late");

    public async Task<IActionResult> OnGetAsync(int? studentId)
    {
        Settings = await _context.SchoolSettings.FirstOrDefaultAsync(settings => settings.Id == 1) ?? new SchoolSettings();
        Students = await _context.Students
            .AsNoTracking()
            .OrderBy(student => student.LastName)
            .ThenBy(student => student.FirstName)
            .ToListAsync();
        if (!_accessService.IsAdmin)
        {
            var allowedClassIds = await _accessService.GetAllowedClassIdsAsync();
            Students = Students.Where(student => student.SchoolClassId.HasValue && allowedClassIds.Contains(student.SchoolClassId.Value)).ToList();
        }

        if (studentId is null)
            return Page();
        if (!await _accessService.CanAccessStudentAsync(studentId.Value))
            return Forbid();

        SelectedStudent = await _context.Students
            .Include(student => student.SchoolClass)
            .AsNoTracking()
            .FirstOrDefaultAsync(student => student.Id == studentId);

        if (SelectedStudent == null)
            return NotFound();

        Grades = await _context.GradeRecords
            .Where(record => record.StudentId == studentId)
            .AsNoTracking()
            .OrderBy(record => record.Subject)
            .ToListAsync();

        Attendance = await _context.AttendanceRecords
            .Where(record => record.StudentId == studentId)
            .AsNoTracking()
            .OrderByDescending(record => record.AttendanceDate)
            .ToListAsync();
        return Page();
    }
}
