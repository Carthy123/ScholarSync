using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScholarSync.Data;
using ScholarSync.Models;
using ScholarSync.Security;
using System.Text;

namespace ScholarSync.Pages.Reports;

public class AcademicModel : PageModel
{
    private readonly SchoolDbContext _context;
    private readonly SchoolAccessService _accessService;
    private readonly AuditService _auditService;

    public AcademicModel(SchoolDbContext context, SchoolAccessService accessService, AuditService auditService)
    {
        _context = context;
        _accessService = accessService;
        _auditService = auditService;
    }

    [BindProperty(SupportsGet = true)] public string? AcademicYear { get; set; }
    [BindProperty(SupportsGet = true)] public int? ClassId { get; set; }
    [BindProperty(SupportsGet = true)] public string? Term { get; set; }
    [BindProperty(SupportsGet = true)] public string? Subject { get; set; }

    public IList<string> AcademicYears { get; private set; } = new List<string>();
    public IList<SchoolClass> Classes { get; private set; } = new List<SchoolClass>();
    public IList<string> Subjects { get; private set; } = new List<string>();
    public IReadOnlyList<string> Terms { get; } = new[] { "First Term", "Second Term", "Third Term" };
    public IList<AcademicSummary> Summaries { get; private set; } = new List<AcademicSummary>();
    public int TotalResults => Summaries.Sum(item => item.ResultCount);
    public decimal OverallAverage => TotalResults == 0 ? 0 : Math.Round(Summaries.Sum(item => item.Average * item.ResultCount) / TotalResults, 1);
    public int PassingResults => Summaries.Sum(item => item.Passing);
    public SchoolSettings Settings { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await _accessService.CanAccessFeatureAsync("Reports"))
            return Forbid();
        await LoadFiltersAsync();
        await LoadResultsAsync();
        return Page();
    }

    public async Task<FileContentResult> OnGetExportCsvAsync()
    {
        await LoadFiltersAsync();
        await LoadResultsAsync();
        var csv = new StringBuilder("Class,Subject,Results,Average,A,B,C,D,F,Pass rate\n");
        foreach (var item in Summaries)
            csv.AppendLine(string.Join(",", Csv(item.ClassName), Csv(item.Subject), item.ResultCount, item.Average.ToString("N1"), item.A, item.B, item.C, item.D, item.F, item.PassRate + "%"));
        await _auditService.LogAsync("Exported", "AcademicReport", details: $"Exported {Summaries.Count} academic summary row(s).");
        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"academic-report-{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    private async Task LoadFiltersAsync()
    {
        Settings = await _context.SchoolSettings.AsNoTracking().FirstOrDefaultAsync(item => item.Id == 1) ?? new SchoolSettings();
        var allowedClassIds = await _accessService.GetAllowedClassIdsAsync();
        var classesQuery = _context.Classes.AsNoTracking();
        if (!_accessService.IsAdmin)
            classesQuery = classesQuery.Where(item => allowedClassIds.Contains(item.Id));
        Classes = ClassOrdering.Order(await classesQuery.ToListAsync()).ToList();
        AcademicYears = Classes.Select(item => item.AcademicYear).Distinct().OrderByDescending(item => item).ToList();
        AcademicYear ??= Settings.AcademicYear;
        if (!AcademicYears.Contains(AcademicYear, StringComparer.OrdinalIgnoreCase))
            AcademicYear = AcademicYears.FirstOrDefault() ?? Settings.AcademicYear;
        if (ClassId.HasValue && !Classes.Any(item => item.Id == ClassId.Value))
            ClassId = null;
        Subjects = await _context.SchoolSubjects.AsNoTracking().Where(item => item.IsActive).OrderBy(item => item.Name).Select(item => item.Name).ToListAsync();
        if (!Terms.Contains(Term, StringComparer.OrdinalIgnoreCase)) Term = null;
        if (!Subjects.Contains(Subject, StringComparer.OrdinalIgnoreCase)) Subject = null;
    }

    private async Task LoadResultsAsync()
    {
        var allowedClassIds = await _accessService.GetAllowedClassIdsAsync();
        var query = _context.GradeRecords
            .Include(record => record.Student)
            .ThenInclude(student => student.SchoolClass)
            .AsNoTracking()
            .Where(record => record.AcademicYear == AcademicYear);
        if (ClassId.HasValue) query = query.Where(record => record.Student.SchoolClassId == ClassId.Value);
        if (!string.IsNullOrWhiteSpace(Term)) query = query.Where(record => record.Term == Term);
        if (!string.IsNullOrWhiteSpace(Subject)) query = query.Where(record => record.Subject == Subject);
        if (!_accessService.IsAdmin) query = query.Where(record => record.Student.SchoolClassId.HasValue && allowedClassIds.Contains(record.Student.SchoolClassId.Value));
        var records = await query.ToListAsync();
        Summaries = records.GroupBy(record => new { ClassName = record.Student.SchoolClass?.Name ?? "Unassigned", record.Subject })
            .Select(group => new AcademicSummary
            {
                ClassName = group.Key.ClassName,
                Subject = group.Key.Subject,
                ResultCount = group.Count(),
                Average = Math.Round(group.Average(record => record.Score), 1),
                A = group.Count(record => GradingScale.GetGrade(record.Score, Settings) == "A"),
                B = group.Count(record => GradingScale.GetGrade(record.Score, Settings) == "B"),
                C = group.Count(record => GradingScale.GetGrade(record.Score, Settings) == "C"),
                D = group.Count(record => GradingScale.GetGrade(record.Score, Settings) == "D"),
                F = group.Count(record => GradingScale.GetGrade(record.Score, Settings) == "F")
            })
            .OrderBy(item => item.ClassName).ThenBy(item => item.Subject)
            .ToList();
    }

    private static string Csv(string value) => $"\"{value.Replace("\"", "\"\"")}\"";

    public sealed class AcademicSummary
    {
        public string ClassName { get; init; } = string.Empty;
        public string Subject { get; init; } = string.Empty;
        public int ResultCount { get; init; }
        public decimal Average { get; init; }
        public int A { get; init; }
        public int B { get; init; }
        public int C { get; init; }
        public int D { get; init; }
        public int F { get; init; }
        public int Passing => A + B + C + D;
        public int PassRate => ResultCount == 0 ? 0 : (int)Math.Round(Passing * 100m / ResultCount);
    }
}
