using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScholarSync.Data;
using ScholarSync.Models;

namespace ScholarSync.Pages.Classes;

public class IndexModel : PageModel
{
    private readonly SchoolDbContext _context;

    public IndexModel(SchoolDbContext context)
    {
        _context = context;
    }

    public IList<SchoolClass> Classes { get; set; } = new List<SchoolClass>();

    public async Task OnGetAsync()
    {
        Classes = await _context.Classes
            .Include(schoolClass => schoolClass.Students)
            .AsNoTracking()
            .OrderBy(schoolClass => schoolClass.Name)
            .ToListAsync();
    }
}
