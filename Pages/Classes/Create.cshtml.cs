using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ScholarSync.Data;
using ScholarSync.Models;

namespace ScholarSync.Pages.Classes;

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
        if (!ModelState.IsValid)
        {
            return Page();
        }

        _context.Classes.Add(SchoolClass);
        await _context.SaveChangesAsync();

        return RedirectToPage("/Classes/Index");
    }
}