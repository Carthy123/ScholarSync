using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScholarSync.Data;
using ScholarSync.Models;
using System.ComponentModel.DataAnnotations;
using ScholarSync.Security;

namespace ScholarSync.Pages.Grades;

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
        if (!ModelState.IsValid)
        {
            await LoadPageDataAsync();
            return Page();
        }
        if (!await _accessService.CanAccessStudentAsync(Input.StudentId))
            return Forbid();

        var grade = new GradeRecord
        {
            StudentId = Input.StudentId,
            Subject = Input.Subject.Trim(),
            Score = Input.Score,
            RecordedAt = DateTime.UtcNow
        };
        _context.GradeRecords.Add(grade);

        await _context.SaveChangesAsync();
        await _auditService.LogAsync("Created", "Grade", grade.Id, $"Recorded {grade.Subject} with score {grade.Score}.");
        return RedirectToPage();
    }

    private async Task LoadPageDataAsync()
    {
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

        Grades = await _context.GradeRecords
            .Include(record => record.Student)
            .AsNoTracking()
            .OrderByDescending(record => record.RecordedAt)
            .ToListAsync();
    }

    public class GradeInput
    {
        [Range(1, int.MaxValue, ErrorMessage = "Select a student.")]
        public int StudentId { get; set; }

        [Required, StringLength(100)]
        public string Subject { get; set; } = string.Empty;

        [Range(0, 100)]
        public decimal Score { get; set; }
    }
}
