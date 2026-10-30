using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScholarSync.Data;
using ScholarSync.Models;
using ScholarSync.Security;

namespace ScholarSync.Pages.Reports;

public class AttendanceModel : PageModel
{
    private readonly SchoolDbContext _context;
    private readonly SchoolAccessService _accessService;

    public AttendanceModel(SchoolDbContext context, SchoolAccessService accessService)
    {
        _context = context;
        _accessService = accessService;
    }

    [BindProperty(SupportsGet = true)] public DateTime StartDate { get; set; } = DateTime.Today.AddDays(-30);
    [BindProperty(SupportsGet = true)] public DateTime EndDate { get; set; } = DateTime.Today;
    [BindProperty(SupportsGet = true)] public int? ClassId { get; set; }

    public IList<SchoolClass> Classes { get; private set; } = new List<SchoolClass>();
    public IList<AttendanceSummary> Summaries { get; private set; } = new List<AttendanceSummary>();
    public int PresentTotal => Summaries.Sum(summary => summary.Present);
    public int AbsentTotal => Summaries.Sum(summary => summary.Absent);
    public int LateTotal => Summaries.Sum(summary => summary.Late);
    public int MarkedTotal => PresentTotal + AbsentTotal + LateTotal;

    public async Task OnGetAsync()
    {
        if (EndDate < StartDate)
            (StartDate, EndDate) = (EndDate, StartDate);

        Classes = await _context.Classes
            .AsNoTracking()
            .ToListAsync();
        Classes = ClassOrdering.Order(Classes).ToList();
        var allowedClassIds = await _accessService.GetAllowedClassIdsAsync();
        if (!_accessService.IsAdmin)
            Classes = Classes.Where(schoolClass => allowedClassIds.Contains(schoolClass.Id)).ToList();

        var studentsQuery = _context.Students
            .Include(student => student.SchoolClass)
            .AsNoTracking();
        if (ClassId.HasValue)
            studentsQuery = studentsQuery.Where(student => student.SchoolClassId == ClassId.Value);
        if (!_accessService.IsAdmin)
            studentsQuery = studentsQuery.Where(student => student.SchoolClassId.HasValue && allowedClassIds.Contains(student.SchoolClassId.Value));

        var students = await studentsQuery
            .OrderBy(student => student.LastName)
            .ThenBy(student => student.FirstName)
            .ToListAsync();
        var studentIds = students.Select(student => student.Id).ToList();
        var records = await _context.AttendanceRecords
            .Where(record => studentIds.Contains(record.StudentId) && record.AttendanceDate >= StartDate.Date && record.AttendanceDate <= EndDate.Date)
            .AsNoTracking()
            .ToListAsync();

        Summaries = students.Select(student =>
        {
            var studentRecords = records.Where(record => record.StudentId == student.Id).ToList();
            return new AttendanceSummary
            {
                StudentName = $"{student.FirstName} {student.LastName}",
                ClassName = student.SchoolClass?.Name ?? "Unassigned",
                Present = studentRecords.Count(record => record.Status == "Present"),
                Absent = studentRecords.Count(record => record.Status == "Absent"),
                Late = studentRecords.Count(record => record.Status == "Late")
            };
        }).ToList();
    }

    public class AttendanceSummary
    {
        public string StudentName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public int Present { get; set; }
        public int Absent { get; set; }
        public int Late { get; set; }
        public int Marked => Present + Absent + Late;
        public int AttendancePercentage => Marked == 0 ? 0 : (int)Math.Round(Present * 100.0 / Marked);
    }
}
