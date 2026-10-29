using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScholarSync.Data;
using ScholarSync.Security;

namespace ScholarSync.Pages.Classes;

[Authorize(Roles = "Admin")]
public class PromoteModel : PageModel
{
    private static readonly IReadOnlyDictionary<string, string> NextClassMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Creche"] = "KG 1",
        ["KG 1"] = "KG 2",
        ["KG 2"] = "Primary 1",
        ["Primary 1"] = "Primary 2",
        ["Primary 2"] = "Primary 3",
        ["Primary 3"] = "Primary 4",
        ["Primary 4"] = "Primary 5",
        ["Primary 5"] = "Primary 6",
        ["Primary 6"] = "JHS 1",
        ["JHS 1"] = "JHS 2",
        ["JHS 2"] = "JHS 3"
    };

    private readonly SchoolDbContext _context;
    private readonly AuditService _auditService;

    public PromoteModel(SchoolDbContext context, AuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public IList<string> AcademicYears { get; private set; } = new List<string>();

    [BindProperty]
    public string SourceYear { get; set; } = string.Empty;

    [BindProperty]
    public string TargetYear { get; set; } = string.Empty;

    public async Task OnGetAsync()
    {
        await LoadYearsAsync();
        SourceYear = AcademicYears.FirstOrDefault() ?? DateTime.UtcNow.Year.ToString();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadYearsAsync();
        SourceYear = SourceYear.Trim();
        TargetYear = TargetYear.Trim();

        if (string.IsNullOrWhiteSpace(SourceYear))
            ModelState.AddModelError(nameof(SourceYear), "Select the current academic year.");
        if (string.IsNullOrWhiteSpace(TargetYear))
            ModelState.AddModelError(nameof(TargetYear), "Enter the new academic year.");
        if (SourceYear == TargetYear)
            ModelState.AddModelError(nameof(TargetYear), "The new academic year must be different from the current year.");

        if (!ModelState.IsValid)
            return Page();

        var sourceClasses = await _context.Classes.Where(schoolClass => schoolClass.AcademicYear == SourceYear).ToListAsync();
        if (sourceClasses.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "No classes were found for the selected academic year.");
            return Page();
        }

        var targetClasses = await _context.Classes.Where(schoolClass => schoolClass.AcademicYear == TargetYear).ToListAsync();
        foreach (var sourceClass in sourceClasses)
        {
            if (!NextClassMap.TryGetValue(sourceClass.Name, out var nextClassName))
                continue;

            if (targetClasses.All(targetClass => targetClass.Name != nextClassName))
            {
                var newClass = new Models.SchoolClass { Name = nextClassName, AcademicYear = TargetYear };
                _context.Classes.Add(newClass);
                targetClasses.Add(newClass);
            }
        }

        await _context.SaveChangesAsync();

        var sourceClassIds = sourceClasses.Select(schoolClass => schoolClass.Id).ToList();
        var students = await _context.Students.Where(student => student.SchoolClassId.HasValue && sourceClassIds.Contains(student.SchoolClassId.Value)).ToListAsync();
        var classMap = targetClasses.ToDictionary(schoolClass => schoolClass.Name, schoolClass => schoolClass.Id);
        var promotedCount = 0;
        foreach (var student in students)
        {
            var sourceClass = sourceClasses.First(schoolClass => schoolClass.Id == student.SchoolClassId);
            if (!NextClassMap.TryGetValue(sourceClass.Name, out var nextClassName))
                continue;

            if (!await _context.StudentEnrollments.AnyAsync(enrollment => enrollment.StudentId == student.Id && enrollment.AcademicYear == SourceYear))
            {
                _context.StudentEnrollments.Add(new Models.StudentEnrollment
                {
                    StudentId = student.Id,
                    SchoolClassId = sourceClass.Id,
                    AcademicYear = SourceYear
                });
            }

            var targetClassId = classMap[nextClassName];
            if (!await _context.StudentEnrollments.AnyAsync(enrollment => enrollment.StudentId == student.Id && enrollment.AcademicYear == TargetYear))
            {
                _context.StudentEnrollments.Add(new Models.StudentEnrollment
                {
                    StudentId = student.Id,
                    SchoolClassId = targetClassId,
                    AcademicYear = TargetYear
                });
            }
            student.SchoolClassId = targetClassId;
            promotedCount++;
        }

        await _context.SaveChangesAsync();
        await _auditService.LogAsync("Promoted", "Students", null, $"Promoted {promotedCount} student(s) from {SourceYear} to {TargetYear}; graduating classes were left without a next-year placement.");
        TempData["PromotionComplete"] = $"{promotedCount} student(s) were moved from {SourceYear} to {TargetYear}. Students in graduating classes were not assigned to an unnecessary class.";
        return RedirectToPage("/Classes/Index");
    }

    private async Task LoadYearsAsync()
    {
        AcademicYears = await _context.Classes
            .AsNoTracking()
            .Select(schoolClass => schoolClass.AcademicYear)
            .Distinct()
            .OrderByDescending(year => year)
            .ToListAsync();
    }
}
