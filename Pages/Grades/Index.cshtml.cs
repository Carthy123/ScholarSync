using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScholarSync.Data;
using ScholarSync.Models;
using System.ComponentModel.DataAnnotations;

namespace ScholarSync.Pages.Grades;

public class IndexModel : PageModel
{
    private readonly SchoolDbContext _context;

    public IndexModel(SchoolDbContext context)
    {
        _context = context;
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

        _context.GradeRecords.Add(new GradeRecord
        {
            StudentId = Input.StudentId,
            Subject = Input.Subject.Trim(),
            Score = Input.Score,
            RecordedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return RedirectToPage();
    }

    private async Task LoadPageDataAsync()
    {
        Students = await _context.Students
            .AsNoTracking()
            .OrderBy(student => student.LastName)
            .ThenBy(student => student.FirstName)
            .ToListAsync();

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
