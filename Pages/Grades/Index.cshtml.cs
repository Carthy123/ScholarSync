using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScholarSync.Data;
using ScholarSync.Models;
using System.ComponentModel.DataAnnotations;
using System.Text;
using ScholarSync.Security;

namespace ScholarSync.Pages.Grades;

public class IndexModel : PageModel
{
    private readonly SchoolDbContext _context;
    private readonly AuditService _auditService;
    private readonly SchoolAccessService _accessService;
    private readonly AcademicYearService _academicYearService;

    public IndexModel(SchoolDbContext context, AuditService auditService, SchoolAccessService accessService, AcademicYearService academicYearService)
    {
        _context = context;
        _auditService = auditService;
        _accessService = accessService;
        _academicYearService = academicYearService;
    }

    [BindProperty]
    public GradeInput Input { get; set; } = new();

    public IList<Student> Students { get; private set; } = new List<Student>();
    public IList<GradeRecord> Grades { get; private set; } = new List<GradeRecord>();
    public IReadOnlyList<string> SubjectOptions { get; } = new[]
    {
        "English Language",
        "Mathematics",
        "Science",
        "Social Studies",
        "Our World Our People (OWOP)",
        "History",
        "Religious and Moral Education",
        "Ghanaian Language",
        "Computing / ICT",
        "Creative Arts",
        "Physical Education",
        "French",
        "Career Technology",
        "Robotics"
    };

    public async Task OnGetAsync()
    {
        await LoadPageDataAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Input.AcademicYear))
            Input.AcademicYear = await _academicYearService.GetCurrentAsync();
        Input.Subject = Input.Subject.Trim();
        Input.Term = Input.Term.Trim();
        if (!SubjectOptions.Contains(Input.Subject, StringComparer.OrdinalIgnoreCase))
            ModelState.AddModelError("Input.Subject", "Select a subject from the approved subject list.");
        if (!new[] { "First Term", "Second Term", "Third Term" }.Contains(Input.Term, StringComparer.OrdinalIgnoreCase))
            ModelState.AddModelError("Input.Term", "Select a valid term.");
        if (!ModelState.IsValid)
        {
            await LoadPageDataAsync();
            return Page();
        }
        if (!await _accessService.CanAccessStudentAsync(Input.StudentId))
            return Forbid();

        var grade = await _context.GradeRecords.FirstOrDefaultAsync(existing =>
            existing.StudentId == Input.StudentId &&
            existing.Subject.ToLower() == Input.Subject.ToLower() &&
            existing.Term == Input.Term &&
            existing.AcademicYear == Input.AcademicYear.Trim());
        var action = "Created";
        if (grade == null)
        {
            grade = new GradeRecord
            {
                StudentId = Input.StudentId,
                Subject = Input.Subject,
                Term = Input.Term,
                AcademicYear = Input.AcademicYear.Trim(),
                Score = Input.Score,
                RecordedAt = DateTime.UtcNow
            };
            _context.GradeRecords.Add(grade);
        }
        else
        {
            grade.Score = Input.Score;
            grade.RecordedAt = DateTime.UtcNow;
            action = "Updated";
        }

        await _context.SaveChangesAsync();
        await _auditService.LogAsync(action, "Grade", grade.Id, $"Recorded {grade.Subject} for {grade.Term}, {grade.AcademicYear} with score {grade.Score}.");
        return RedirectToPage();
    }

    public async Task<FileContentResult> OnGetExportCsvAsync()
    {
        var allowedClassIds = await _accessService.GetAllowedClassIdsAsync();
        IQueryable<GradeRecord> query = _context.GradeRecords.Include(record => record.Student).ThenInclude(student => student.SchoolClass).AsNoTracking();
        if (!_accessService.IsAdmin)
            query = query.Where(record => record.Student.SchoolClassId.HasValue && allowedClassIds.Contains(record.Student.SchoolClassId.Value));
        var grades = await query.OrderBy(record => record.Student.LastName).ThenBy(record => record.Student.FirstName).ThenBy(record => record.Subject).ToListAsync();
        var csv = new StringBuilder("Student ID,First Name,Last Name,Class,Subject,Term,Academic Year,Score,Grade,Recorded At\n");
        foreach (var record in grades)
            csv.AppendLine(string.Join(",", CsvValue(record.StudentId.ToString()), CsvValue(record.Student.FirstName), CsvValue(record.Student.LastName), CsvValue(record.Student.SchoolClass?.Name ?? "Unassigned"), CsvValue(record.Subject), CsvValue(record.Term), CsvValue(record.AcademicYear), CsvValue(record.Score.ToString("N2")), CsvValue(record.Grade), CsvValue(record.RecordedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"))));
        await _auditService.LogAsync("Exported", "Grade", details: $"Exported {grades.Count} grade record(s).");
        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"grades-{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    private static string CsvValue(string? value) => $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";

    private async Task LoadPageDataAsync()
    {
        var allowedClassIds = await _accessService.GetAllowedClassIdsAsync();
        Students = await _context.Students
            .AsNoTracking()
            .OrderBy(student => student.LastName)
            .ThenBy(student => student.FirstName)
            .ToListAsync();
        if (!_accessService.IsAdmin)
        {
            Students = Students.Where(student => student.SchoolClassId.HasValue && allowedClassIds.Contains(student.SchoolClassId.Value)).ToList();
        }

        IQueryable<GradeRecord> gradesQuery = _context.GradeRecords
            .Include(record => record.Student)
            .AsNoTracking();
        if (!_accessService.IsAdmin)
            gradesQuery = gradesQuery.Where(record => record.Student.SchoolClassId.HasValue && allowedClassIds.Contains(record.Student.SchoolClassId.Value));
        Grades = await gradesQuery
            .OrderByDescending(record => record.RecordedAt)
            .ToListAsync();
        if (string.IsNullOrWhiteSpace(Input.AcademicYear))
            Input.AcademicYear = await _academicYearService.GetCurrentAsync();
    }

    public class GradeInput
    {
        [Range(1, int.MaxValue, ErrorMessage = "Select a student.")]
        public int StudentId { get; set; }

        [Required, StringLength(100)]
        public string Subject { get; set; } = string.Empty;

        [Required, StringLength(30)]
        public string Term { get; set; } = "First Term";

        [Required, StringLength(30)]
        public string AcademicYear { get; set; } = string.Empty;

        [Range(0, 100)]
        public decimal Score { get; set; }
    }
}
