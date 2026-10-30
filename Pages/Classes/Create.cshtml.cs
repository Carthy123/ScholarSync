using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScholarSync.Data;
using ScholarSync.Models;
using Microsoft.AspNetCore.Authorization;

namespace ScholarSync.Pages.Classes;

[Authorize(Roles = "Admin")]
public class CreateModel : PageModel
{
    private readonly SchoolDbContext _context;

    public CreateModel(SchoolDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public SchoolClass SchoolClass { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        SchoolClass.Name = SchoolClass.Name.Trim();
        SchoolClass.AcademicYear = SchoolClass.AcademicYear.Trim();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var duplicateExists = await _context.Classes
            .AsNoTracking()
            .Where(schoolClass => schoolClass.AcademicYear == SchoolClass.AcademicYear)
            .AnyAsync(schoolClass => schoolClass.Name.ToLower() == SchoolClass.Name.ToLower());

        if (duplicateExists)
        {
            ModelState.AddModelError(nameof(SchoolClass.Name), $"A class named '{SchoolClass.Name}' already exists for academic year {SchoolClass.AcademicYear}.");
            return Page();
        }

        _context.Classes.Add(SchoolClass);
        await _context.SaveChangesAsync();

        return RedirectToPage("/Classes/Index");
    }
}
