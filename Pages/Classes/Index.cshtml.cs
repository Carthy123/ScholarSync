using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using ScholarSync.Data;
using ScholarSync.Models;
using ScholarSync.Security;

namespace ScholarSync.Pages.Classes;

public class IndexModel : PageModel
{
    private readonly SchoolDbContext _context;
    private readonly SchoolAccessService _accessService;
    private readonly AcademicYearService _academicYearService;

    public IndexModel(SchoolDbContext context, SchoolAccessService accessService, AcademicYearService academicYearService)
    {
        _context = context;
        _accessService = accessService;
        _academicYearService = academicYearService;
    }

    public IList<SchoolClass> Classes { get; set; } = new List<SchoolClass>();
    public IList<string> AcademicYears { get; private set; } = new List<string>();

    [BindProperty(SupportsGet = true)]
    public string? AcademicYear { get; set; }

    public async Task OnGetAsync()
    {
        AcademicYears = await _context.Classes
            .AsNoTracking()
            .Select(schoolClass => schoolClass.AcademicYear)
            .Distinct()
            .OrderByDescending(year => year)
            .ToListAsync();

        AcademicYear ??= await _academicYearService.GetCurrentAsync();
        if (!AcademicYears.Contains(AcademicYear))
            AcademicYear = AcademicYears.FirstOrDefault();

        Classes = await _context.Classes
            .Include(schoolClass => schoolClass.Students)
            .AsNoTracking()
            .Where(schoolClass => schoolClass.AcademicYear == AcademicYear)
            .ToListAsync();
        Classes = ClassOrdering.Order(Classes).ToList();
        if (!_accessService.IsAdmin)
        {
            var allowedClassIds = await _accessService.GetAllowedClassIdsAsync();
            Classes = Classes.Where(schoolClass => allowedClassIds.Contains(schoolClass.Id)).ToList();
        }
    }
}
