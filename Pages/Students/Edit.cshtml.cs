using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScholarSync.Data;
using ScholarSync.Models;

namespace ScholarSync.Pages.Students;

public class EditModel : PageModel
{
    private readonly SchoolDbContext _context;

    public EditModel(SchoolDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Student Student { get; set; } = new();

    public IList<SchoolClass> Classes { get; set; } = new List<SchoolClass>();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var student = await _context.Students.FindAsync(id);

        if (student == null)
        {
            return NotFound();
        }

        Student = student;
        await LoadClassesAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadClassesAsync();
            return Page();
        }

        _context.Students.Update(Student);
        await _context.SaveChangesAsync();

        return RedirectToPage("/Students/Index");
    }

    private async Task LoadClassesAsync()
    {
        Classes = await _context.Classes
            .OrderBy(schoolClass => schoolClass.Name)
            .ToListAsync();
    }
}
