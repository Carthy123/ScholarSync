using Microsoft.AspNetCore.Mvc;
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
    public IList<SchoolEvent> UpcomingEvents { get; private set; } = new List<SchoolEvent>();
    public decimal OutstandingFees { get; private set; }
    public int StudentsWithOutstandingFees { get; private set; }
    public int LockedAccountCount { get; private set; }
    public IList<TeacherWorkloadRow> TeacherWorkload { get; private set; } = new List<TeacherWorkloadRow>();
    public IList<SchoolAnnouncement> Announcements { get; private set; } = new List<SchoolAnnouncement>();
    public decimal ExpectedFees { get; private set; }
    public decimal AverageGradeScore { get; private set; }
    public int PassingGradeCount { get; private set; }
    public int GradedStudentCount { get; private set; }
    public IList<AttendanceTrendPoint> AttendanceTrend { get; private set; } = new List<AttendanceTrendPoint>();
    public IList<StudentPerformanceRow> TopPerformers { get; private set; } = new List<StudentPerformanceRow>();
    public IList<StudentPerformanceRow> TopAttendance { get; private set; } = new List<StudentPerformanceRow>();
    public IList<SubjectPerformanceRow> SubjectPerformance { get; private set; } = new List<SubjectPerformanceRow>();
    [BindProperty(SupportsGet = true)] public string? DashboardAcademicYear { get; set; }
    [BindProperty(SupportsGet = true)] public int? DashboardClassId { get; set; }
    public IList<string> DashboardAcademicYears { get; private set; } = new List<string>();
    public IList<SchoolClass> DashboardClasses { get; private set; } = new List<SchoolClass>();

    public async Task OnGetAsync()
    {
        Settings = await _context.SchoolSettings.FirstOrDefaultAsync(settings => settings.Id == 1) ?? new SchoolSettings();
        var allowedClassIds = await _accessService.GetAllowedClassIdsAsync();
        var permittedClassesQuery = _context.Classes.AsNoTracking();
        if (!_accessService.IsAdmin)
            permittedClassesQuery = permittedClassesQuery.Where(schoolClass => allowedClassIds.Contains(schoolClass.Id));
        var permittedClasses = await permittedClassesQuery.ToListAsync();
        DashboardAcademicYears = permittedClasses
            .Select(schoolClass => schoolClass.AcademicYear)
            .Where(year => !string.IsNullOrWhiteSpace(year))
            .Distinct()
            .OrderByDescending(year => year)
            .ToList();
        DashboardAcademicYear = string.IsNullOrWhiteSpace(DashboardAcademicYear) ? Settings.AcademicYear : DashboardAcademicYear.Trim();
        if (!DashboardAcademicYears.Contains(DashboardAcademicYear, StringComparer.OrdinalIgnoreCase) && DashboardAcademicYears.Count > 0)
            DashboardAcademicYear = DashboardAcademicYears[0];
        DashboardClasses = ClassOrdering.Order(permittedClasses
            .Where(schoolClass => string.Equals(schoolClass.AcademicYear, DashboardAcademicYear, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        var dashboardClassIds = DashboardClasses.Select(schoolClass => schoolClass.Id).ToList();
        if (DashboardClassId.HasValue && !dashboardClassIds.Contains(DashboardClassId.Value))
            DashboardClassId = null;
        var selectedClassIds = DashboardClassId.HasValue ? new List<int> { DashboardClassId.Value } : dashboardClassIds;
        var visibleStudentIds = await _context.Students
            .Where(student => student.SchoolClassId.HasValue && selectedClassIds.Contains(student.SchoolClassId.Value))
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
        Announcements = await _context.SchoolAnnouncements.AsNoTracking()
            .Where(item => (!item.ExpiresAt.HasValue || item.ExpiresAt > DateTime.UtcNow) && (item.Audience == "Everyone" || (item.Audience == "Teachers" && User.IsInRole("Teacher")) || (item.Audience == "Administrators" && User.IsInRole("Admin"))))
            .OrderByDescending(item => item.IsPinned).ThenByDescending(item => item.Priority == "Urgent").ThenByDescending(item => item.CreatedAt)
            .Take(3)
            .ToListAsync();
        StudentCount = visibleStudentIds.Count;
        ClassCount = selectedClassIds.Count;
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

        UpcomingEvents = await _context.SchoolEvents
            .AsNoTracking()
            .Where(schoolEvent => schoolEvent.EndDate >= today)
            .OrderBy(schoolEvent => schoolEvent.StartDate)
            .ThenBy(schoolEvent => schoolEvent.Title)
            .Take(5)
            .ToListAsync();

        if (_accessService.IsAdmin)
        {
            LockedAccountCount = await _context.AdminUsers.CountAsync(user => user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow);
        }

        var visibleStudents = await _context.Students
            .AsNoTracking()
            .Where(student => visibleStudentIds.Contains(student.Id) && student.SchoolClassId.HasValue)
            .Select(student => new { student.Id, student.SchoolClassId })
            .ToListAsync();
        var feeStructures = await _context.FeeStructures
            .AsNoTracking()
            .Where(structure => structure.AcademicYear == Settings.AcademicYear && visibleStudents.Select(student => student.SchoolClassId!.Value).Contains(structure.SchoolClassId))
            .ToListAsync();
        var currentPaymentRows = await _context.FeePayments
            .AsNoTracking()
            .Where(payment => payment.AcademicYear == Settings.AcademicYear && visibleStudentIds.Contains(payment.StudentId))
            .Select(payment => new { payment.StudentId, payment.Amount })
            .ToListAsync();
        var currentPayments = currentPaymentRows
            .GroupBy(payment => payment.StudentId)
            .Select(group => new { StudentId = group.Key, Total = group.Sum(payment => payment.Amount) })
            .ToList();
        var paidByStudent = currentPayments.ToDictionary(payment => payment.StudentId, payment => payment.Total);
        foreach (var student in visibleStudents)
        {
            var expected = feeStructures.Where(structure => structure.SchoolClassId == student.SchoolClassId!.Value).Sum(structure => structure.Amount);
            ExpectedFees += expected;
            paidByStudent.TryGetValue(student.Id, out var paid);
            var outstanding = Math.Max(expected - paid, 0);
            OutstandingFees += outstanding;
            if (outstanding > 0)
                StudentsWithOutstandingFees++;
        }

        var trendStart = today.AddDays(-6);
        var attendanceRows = await _context.AttendanceRecords
            .AsNoTracking()
            .Where(record => visibleStudentIds.Contains(record.StudentId) && record.AttendanceDate >= trendStart && record.AttendanceDate <= today)
            .Select(record => new { record.AttendanceDate, record.Status })
            .ToListAsync();
        AttendanceTrend = Enumerable.Range(0, 7).Select(offset =>
        {
            var date = trendStart.AddDays(offset);
            return new AttendanceTrendPoint
            {
                Date = date,
                Present = attendanceRows.Count(row => row.AttendanceDate == date && row.Status == "Present"),
                Absent = attendanceRows.Count(row => row.AttendanceDate == date && row.Status == "Absent"),
                Late = attendanceRows.Count(row => row.AttendanceDate == date && row.Status == "Late")
            };
        }).ToList();

        var dashboardGrades = await _context.GradeRecords
            .Include(record => record.Student)
            .ThenInclude(student => student.SchoolClass)
            .AsNoTracking()
            .Where(record => visibleStudentIds.Contains(record.StudentId) && record.AcademicYear == Settings.AcademicYear)
            .ToListAsync();
        var gradeScores = dashboardGrades.Select(record => record.Score).ToList();
        if (gradeScores.Count > 0)
        {
            AverageGradeScore = gradeScores.Average();
            PassingGradeCount = gradeScores.Count(score => score >= 50);
            GradedStudentCount = gradeScores.Count;
        }

        TopPerformers = dashboardGrades
            .GroupBy(record => record.StudentId)
            .Select(group => new StudentPerformanceRow
            {
                StudentId = group.Key,
                Name = $"{group.First().Student.FirstName} {group.First().Student.LastName}",
                Initials = Initials(group.First().Student),
                PhotoPath = group.First().Student.PhotoPath,
                ClassName = group.First().Student.SchoolClass?.Name ?? "Unassigned",
                Score = Math.Round(group.Average(record => record.Score), 1),
                RecordCount = group.Count(),
                MetricLabel = "Average mark"
            })
            .OrderByDescending(row => row.Score)
            .Take(4)
            .ToList();

        SubjectPerformance = dashboardGrades
            .GroupBy(record => record.Subject)
            .Select(group => new SubjectPerformanceRow
            {
                Subject = group.Key,
                Average = Math.Round(group.Average(record => record.Score), 1),
                Passing = group.Count(record => record.Score >= 50),
                Failing = group.Count(record => record.Score < 50)
            })
            .OrderByDescending(row => row.Average)
            .Take(6)
            .ToList();

        var attendanceWindowStart = today.AddDays(-29);
        var attendanceDetails = await _context.AttendanceRecords
            .Include(record => record.Student)
            .ThenInclude(student => student.SchoolClass)
            .AsNoTracking()
            .Where(record => visibleStudentIds.Contains(record.StudentId) && record.AttendanceDate >= attendanceWindowStart && record.AttendanceDate <= today)
            .ToListAsync();
        TopAttendance = attendanceDetails
            .GroupBy(record => record.StudentId)
            .Where(group => group.Any())
            .Select(group => new StudentPerformanceRow
            {
                StudentId = group.Key,
                Name = $"{group.First().Student.FirstName} {group.First().Student.LastName}",
                Initials = Initials(group.First().Student),
                PhotoPath = group.First().Student.PhotoPath,
                ClassName = group.First().Student.SchoolClass?.Name ?? "Unassigned",
                Score = Math.Round(group.Count(record => record.Status == "Present") * 100m / group.Count(), 1),
                RecordCount = group.Count(),
                MetricLabel = "Attendance"
            })
            .OrderByDescending(row => row.Score)
            .Take(4)
            .ToList();

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
            .Where(schoolClass => selectedClassIds.Contains(schoolClass.Id))
            .ToListAsync();
        ClassSummaries = ClassOrdering.Order(ClassSummaries).Take(8).ToList();

        if (User.IsInRole("Teacher"))
        {
            var assignedClassIds = ClassSummaries.Select(schoolClass => schoolClass.Id).ToList();
            var teacherStudents = await _context.Students.AsNoTracking()
                .Where(student => student.SchoolClassId.HasValue && assignedClassIds.Contains(student.SchoolClassId.Value))
                .Select(student => new { student.Id, ClassId = student.SchoolClassId!.Value }).ToListAsync();
            var teacherStudentIds = teacherStudents.Select(student => student.Id).ToList();
            var markedAttendanceStudentIds = await _context.AttendanceRecords
                .Where(record => record.AttendanceDate == today && teacherStudentIds.Contains(record.StudentId))
                .Select(record => record.StudentId).ToListAsync();
            var gradeEntries = await _context.GradeRecords
                .Where(record => record.AcademicYear == Settings.AcademicYear && teacherStudentIds.Contains(record.StudentId))
                .GroupBy(record => record.StudentId)
                .Select(group => new { StudentId = group.Key, Count = group.Count() }).ToListAsync();
            var gradeCountByStudent = gradeEntries.ToDictionary(item => item.StudentId, item => item.Count);
            TeacherWorkload = ClassSummaries.Select(schoolClass =>
            {
                var classStudentIds = teacherStudents.Where(student => student.ClassId == schoolClass.Id).Select(student => student.Id).ToList();
                return new TeacherWorkloadRow
                {
                    ClassName = schoolClass.Name,
                    AcademicYear = schoolClass.AcademicYear,
                    StudentCount = classStudentIds.Count,
                    AttendanceMarked = classStudentIds.Count(markedAttendanceStudentIds.Contains),
                    GradeEntries = classStudentIds.Sum(studentId => gradeCountByStudent.GetValueOrDefault(studentId))
                };
            }).ToList();
        }
    }

    public sealed class AttendanceTrendPoint
    {
        public DateTime Date { get; init; }
        public int Present { get; init; }
        public int Absent { get; init; }
        public int Late { get; init; }
        public int Marked => Present + Absent + Late;
    }

    public sealed class TeacherWorkloadRow
    {
        public string ClassName { get; init; } = string.Empty;
        public string AcademicYear { get; init; } = string.Empty;
        public int StudentCount { get; init; }
        public int AttendanceMarked { get; init; }
        public int GradeEntries { get; init; }
        public int AttendancePercentage => StudentCount == 0 ? 0 : (int)Math.Round(AttendanceMarked * 100m / StudentCount);
    }

    public sealed class StudentPerformanceRow
    {
        public int StudentId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Initials { get; init; } = "SS";
        public string? PhotoPath { get; init; }
        public string ClassName { get; init; } = string.Empty;
        public decimal Score { get; init; }
        public int RecordCount { get; init; }
        public string MetricLabel { get; init; } = string.Empty;
    }

    public sealed class SubjectPerformanceRow
    {
        public string Subject { get; init; } = string.Empty;
        public decimal Average { get; init; }
        public int Passing { get; init; }
        public int Failing { get; init; }
    }

    private static string Initials(Student student)
    {
        var first = string.IsNullOrWhiteSpace(student.FirstName) ? "S" : student.FirstName[..1];
        var last = string.IsNullOrWhiteSpace(student.LastName) ? "S" : student.LastName[..1];
        return $"{first}{last}".ToUpperInvariant();
    }
}
