using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScholarSync.Data;
using ScholarSync.Models;

namespace ScholarSync.Pages.Students;

public class CreateModel : PageModel
{
    private readonly SchoolDbContext _context;

    public CreateModel(SchoolDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Student Student { get; set; } = new();

    public IList<SchoolClass> Classes { get; set; } = new List<SchoolClass>();

    public async Task OnGetAsync()
    {
        Classes = await _context.Classes
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            Classes = await _context.Classes
                .OrderBy(c => c.Name)
                .ToListAsync();

            return Page();
        }

        _context.Students.Add(Student);
        await _context.SaveChangesAsync();

        return RedirectToPage("/Students/Index");
    }
}