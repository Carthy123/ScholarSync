using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScholarSync.Data;
using ScholarSync.Models;
using ScholarSync.Security;

namespace ScholarSync.Pages.Classes;

public class PrintModel : PageModel
{
    private readonly SchoolDbContext _context;
    private readonly SchoolAccessService _accessService;

    public PrintModel(SchoolDbContext context, SchoolAccessService accessService)
    {
        _context = context;
        _accessService = accessService;
    }

    public SchoolClass? SelectedClass { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        SelectedClass = await _context.Classes
            .Include(schoolClass => schoolClass.Students)
            .AsNoTracking()
            .FirstOrDefaultAsync(schoolClass => schoolClass.Id == id);

        if (SelectedClass == null)
            return NotFound();
        if (!await _accessService.CanAccessClassAsync(id))
            return Forbid();
        return Page();
    }
}
