using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScholarSync.Data;
using ScholarSync.Models;
using System.Text;
using ScholarSync.Security;

namespace ScholarSync.Pages.Students;

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

    public IList<Student> Students { get; set; } = new List<Student>();
    public IList<SchoolClass> Classes { get; private set; } = new List<SchoolClass>();
    public int TotalStudents { get; private set; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalStudents / (double)PageSize));

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? ClassId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? AcademicYear { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public bool ShowArchived { get; set; }

    public int PageSize { get; } = 10;

    public async Task OnGetAsync()
    {
        if (ShowArchived && !_accessService.IsAdmin)
            ShowArchived = false;

        var query = _context.Students
            .IgnoreQueryFilters()
            .Include(student => student.SchoolClass)
            .AsNoTracking();
        query = query.Where(student => ShowArchived || !student.IsArchived);
        var allowedClassIds = await _accessService.GetAllowedClassIdsAsync();
        if (!_accessService.IsAdmin)
            query = query.Where(student => student.SchoolClassId.HasValue && allowedClassIds.Contains(student.SchoolClassId.Value));

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var search = SearchTerm.Trim();
            query = query.Where(student =>
                student.FirstName.Contains(search) ||
                student.LastName.Contains(search) ||
                (student.GuardianName != null && student.GuardianName.Contains(search)) ||
                (student.GuardianPhone != null && student.GuardianPhone.Contains(search)));
        }

        if (ClassId.HasValue)
            query = query.Where(student => student.SchoolClassId == ClassId.Value);

        if (!string.IsNullOrWhiteSpace(AcademicYear))
            query = query.Where(student => student.SchoolClass != null && student.SchoolClass.AcademicYear == AcademicYear);

        TotalStudents = await query.CountAsync();
        PageNumber = Math.Clamp(PageNumber, 1, TotalPages);
        Students = await query
            .OrderBy(student => student.LastName)
            .ThenBy(student => student.FirstName)
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();
        Classes = await _context.Classes
            .AsNoTracking()
            .ToListAsync();
        Classes = ClassOrdering.Order(Classes).ToList();
    }

    public async Task<IActionResult> OnPostArchiveAsync(int id)
    {
        if (!_accessService.IsAdmin)
            return Forbid();

        var student = await _context.Students.IgnoreQueryFilters().FirstOrDefaultAsync(student => student.Id == id);

        if (student != null && !student.IsArchived)
        {
            student.IsArchived = true;
            student.ArchivedAt = DateTime.UtcNow;
            student.ArchivedBy = User.Identity?.Name;
            await _context.SaveChangesAsync();
            await _auditService.LogAsync("Archived", "Student", id, $"Archived {student.FirstName} {student.LastName}.");
        }

        return RedirectToPage(new { ShowArchived });
    }

    public async Task<IActionResult> OnPostUnarchiveAsync(int id)
    {
        if (!_accessService.IsAdmin)
            return Forbid();

        var student = await _context.Students.IgnoreQueryFilters().FirstOrDefaultAsync(student => student.Id == id);
        if (student != null && student.IsArchived)
        {
            student.IsArchived = false;
            student.ArchivedAt = null;
            student.ArchivedBy = null;
            await _context.SaveChangesAsync();
            await _auditService.LogAsync("Unarchived", "Student", id, $"Restored {student.FirstName} {student.LastName}.");
        }

        return RedirectToPage(new { ShowArchived = true });
    }

    public async Task<FileContentResult> OnGetExportCsvAsync()
    {
        var students = await _context.Students
            .Include(student => student.SchoolClass)
            .AsNoTracking()
            .OrderBy(student => student.LastName)
            .ThenBy(student => student.FirstName)
            .ToListAsync();
        if (!_accessService.IsAdmin)
        {
            var allowedClassIds = await _accessService.GetAllowedClassIdsAsync();
            students = students.Where(student => student.SchoolClassId.HasValue && allowedClassIds.Contains(student.SchoolClassId.Value)).ToList();
        }

        var csv = new StringBuilder();
        csv.AppendLine("First Name,Last Name,Date of Birth,Gender,Guardian,Phone,Class,Academic Year");

        foreach (var student in students)
        {
            csv.AppendLine(string.Join(",", new[]
            {
                CsvValue(student.FirstName),
                CsvValue(student.LastName),
                CsvValue(student.DateOfBirth?.ToString("yyyy-MM-dd")),
                CsvValue(student.Gender),
                CsvValue(student.GuardianName),
                CsvValue(student.GuardianPhone),
                CsvValue(student.SchoolClass?.Name ?? "Unassigned"),
                CsvValue(student.SchoolClass?.AcademicYear)
            }));
        }

        return File(
            Encoding.UTF8.GetBytes(csv.ToString()),
            "text/csv",
            $"students-{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    private static string CsvValue(string? value)
    {
        var escaped = (value ?? string.Empty).Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }
}
