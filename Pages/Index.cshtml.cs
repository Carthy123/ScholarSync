using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ScholarSync.Security;
using ScholarSync.Data;
using ScholarSync.Models;

namespace ScholarSync.Pages;

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
    public int ClassCount { get; private set; }
    public int UnassignedStudentCount { get; private set; }
    public int TodayPresentCount { get; private set; }
    public int TodayAbsentCount { get; private set; }
    public int TodayLateCount { get; private set; }
    public decimal TotalFeesReceived { get; private set; }
    public int AttendanceCompletionPercentage => StudentCount == 0 ? 0 : (int)Math.Round((TodayPresentCount + TodayAbsentCount + TodayLateCount) * 100m / StudentCount);
    public IList<Student> RecentStudents { get; private set; } = new List<Student>();
    public IList<GradeRecord> RecentGrades { get; private set; } = new List<GradeRecord>();
    public IList<SchoolClass> ClassSummaries { get; private set; } = new List<SchoolClass>();
    public SchoolSettings Settings { get; private set; } = new();
    public IList<SchoolClass> AssignedClasses { get; private set; } = new List<SchoolClass>();

    public async Task OnGetAsync()
    {
        Settings = await _context.SchoolSettings.FirstOrDefaultAsync(settings => settings.Id == 1) ?? new SchoolSettings();
        var allowedClassIds = await _accessService.GetAllowedClassIdsAsync();
        var visibleStudentIds = await _context.Students
            .Where(student => _accessService.IsAdmin || (student.SchoolClassId.HasValue && allowedClassIds.Contains(student.SchoolClassId.Value)))
            .Select(student => student.Id)
            .ToListAsync();
        if (User.IsInRole("Teacher") && int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var teacherId))
        {
            AssignedClasses = await _context.ClassTeachers
                .Where(assignment => assignment.AdminUserId == teacherId)
                .Include(assignment => assignment.SchoolClass)
                .Select(assignment => assignment.SchoolClass)
                .AsNoTracking()
                .OrderBy(schoolClass => schoolClass.AcademicYear)
                .ToListAsync();
            AssignedClasses = ClassOrdering.Order(AssignedClasses).ToList();
        }
        StudentCount = visibleStudentIds.Count;
        ClassCount = _accessService.IsAdmin ? await _context.Classes.CountAsync() : allowedClassIds.Count;
        UnassignedStudentCount = _accessService.IsAdmin ? await _context.Students.CountAsync(student => student.SchoolClassId == null) : 0;
        var today = DateTime.Today;
        TodayPresentCount = await _context.AttendanceRecords.CountAsync(record => visibleStudentIds.Contains(record.StudentId) && record.AttendanceDate == today && record.Status == "Present");
        TodayAbsentCount = await _context.AttendanceRecords.CountAsync(record => visibleStudentIds.Contains(record.StudentId) && record.AttendanceDate == today && record.Status == "Absent");
        TodayLateCount = await _context.AttendanceRecords.CountAsync(record => visibleStudentIds.Contains(record.StudentId) && record.AttendanceDate == today && record.Status == "Late");
        var feeAmounts = await _context.FeePayments
            .AsNoTracking()
            .Where(payment => visibleStudentIds.Contains(payment.StudentId))
            .Select(payment => payment.Amount)
            .ToListAsync();
        TotalFeesReceived = feeAmounts.Sum();
        RecentStudents = await _context.Students
            .Include(student => student.SchoolClass)
            .AsNoTracking()
            .Where(student => visibleStudentIds.Contains(student.Id))
            .OrderByDescending(student => student.Id)
            .Take(5)
            .ToListAsync();
        RecentGrades = await _context.GradeRecords
            .Include(record => record.Student)
            .AsNoTracking()
            .Where(record => visibleStudentIds.Contains(record.StudentId))
            .OrderByDescending(record => record.RecordedAt)
            .Take(5)
            .ToListAsync();
        ClassSummaries = await _context.Classes
            .Include(schoolClass => schoolClass.Students)
            .AsNoTracking()
            .Where(schoolClass => _accessService.IsAdmin || allowedClassIds.Contains(schoolClass.Id))
            .ToListAsync();
        ClassSummaries = ClassOrdering.Order(ClassSummaries).Take(8).ToList();
    }
}
