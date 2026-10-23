using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScholarSync.Data;
using ScholarSync.Models;

namespace ScholarSync.Pages.Students;

public class DetailsModel : PageModel
{
    private readonly SchoolDbContext _context;

    public DetailsModel(SchoolDbContext context)
    {
        _context = context;
    }

    public Student Student { get; private set; } = null!;
    public IList<GradeRecord> Grades { get; private set; } = new List<GradeRecord>();
    public IList<AttendanceRecord> Attendance { get; private set; } = new List<AttendanceRecord>();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var student = await _context.Students
            .Include(student => student.SchoolClass)
            .AsNoTracking()
            .FirstOrDefaultAsync(student => student.Id == id);

        if (student == null)
        {
            return NotFound();
        }

        Student = student;

        Grades = await _context.GradeRecords
            .Where(record => record.StudentId == id)
            .AsNoTracking()
            .OrderByDescending(record => record.RecordedAt)
            .ToListAsync();

        Attendance = await _context.AttendanceRecords
            .Where(record => record.StudentId == id)
            .AsNoTracking()
            .OrderByDescending(record => record.AttendanceDate)
            .Take(10)
            .ToListAsync();

        return Page();
    }
}
