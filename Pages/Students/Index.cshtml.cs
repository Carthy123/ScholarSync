using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScholarSync.Data;
using ScholarSync.Models;
using System.Text;

namespace ScholarSync.Pages.Students;

public class IndexModel : PageModel
{
    private readonly SchoolDbContext _context;

    public IndexModel(SchoolDbContext context)
    {
        _context = context;
    }

    public IList<Student> Students { get; set; } = new List<Student>();

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    public async Task OnGetAsync()
    {
        var query = _context.Students
            .Include(student => student.SchoolClass)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var search = SearchTerm.Trim();
            query = query.Where(student =>
                student.FirstName.Contains(search) ||
                student.LastName.Contains(search) ||
                (student.GuardianName != null && student.GuardianName.Contains(search)) ||
                (student.GuardianPhone != null && student.GuardianPhone.Contains(search)));
        }

        Students = await query.ToListAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var student = await _context.Students.FindAsync(id);

        if (student != null)
        {
            _context.Students.Remove(student);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage();
    }

    public async Task<FileContentResult> OnGetExportCsvAsync()
    {
        var students = await _context.Students
            .Include(student => student.SchoolClass)
            .AsNoTracking()
            .OrderBy(student => student.LastName)
            .ThenBy(student => student.FirstName)
            .ToListAsync();

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
