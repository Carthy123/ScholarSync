using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScholarSync.Data;
using ScholarSync.Models;
using ScholarSync.Security;

namespace ScholarSync.Pages.Fees;

[Authorize(Roles = "Admin")]
public class StructuresModel : PageModel
{
    private readonly SchoolDbContext _context;
    private readonly AuditService _auditService;
    private readonly AcademicYearService _academicYearService;

    public StructuresModel(SchoolDbContext context, AuditService auditService, AcademicYearService academicYearService)
    {
        _context = context;
        _auditService = auditService;
        _academicYearService = academicYearService;
    }

    [BindProperty]
    public StructureInput Input { get; set; } = new();

    public IList<SchoolClass> Classes { get; private set; } = new List<SchoolClass>();
    public IList<FeeStructure> Structures { get; private set; } = new List<FeeStructure>();
    public IReadOnlyList<string> Terms { get; } = new[] { "First Term", "Second Term", "Third Term" };

    public async Task OnGetAsync() => await LoadAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Input.AcademicYear))
            Input.AcademicYear = await _academicYearService.GetCurrentAsync();
        if (await _context.FeeStructures.AnyAsync(item => item.SchoolClassId == Input.SchoolClassId && item.Term == Input.Term && item.AcademicYear == Input.AcademicYear.Trim()))
            ModelState.AddModelError(string.Empty, "A fee structure already exists for this class, term, and academic year.");

        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        var structure = new FeeStructure
        {
            SchoolClassId = Input.SchoolClassId,
            Term = Input.Term,
            AcademicYear = Input.AcademicYear.Trim(),
            Amount = Input.Amount,
            Description = string.IsNullOrWhiteSpace(Input.Description) ? null : Input.Description.Trim()
        };
        _context.FeeStructures.Add(structure);
        await _context.SaveChangesAsync();
        await _auditService.LogAsync("Created", "FeeStructure", structure.Id, $"Set GHS {structure.Amount:N2} for {structure.Term}.");
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var structure = await _context.FeeStructures.FindAsync(id);
        if (structure != null)
        {
            _context.FeeStructures.Remove(structure);
            await _context.SaveChangesAsync();
            await _auditService.LogAsync("Deleted", "FeeStructure", id, "Removed a fee structure.");
        }
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        Classes = await _context.Classes.AsNoTracking().OrderBy(item => item.AcademicYear).ThenBy(item => item.Name).ToListAsync();
        Structures = await _context.FeeStructures.Include(item => item.SchoolClass).AsNoTracking().OrderByDescending(item => item.AcademicYear).ThenBy(item => item.SchoolClass.Name).ThenBy(item => item.Term).ToListAsync();
        if (string.IsNullOrWhiteSpace(Input.AcademicYear))
            Input.AcademicYear = await _academicYearService.GetCurrentAsync();
    }

    public class StructureInput
    {
        [Range(1, int.MaxValue, ErrorMessage = "Select a class.")] public int SchoolClassId { get; set; }
        [Required] public string Term { get; set; } = "First Term";
        [Required, StringLength(30)] public string AcademicYear { get; set; } = string.Empty;
        [Range(0.01, 100000000, ErrorMessage = "Enter an amount greater than zero.")] public decimal Amount { get; set; }
        [StringLength(150)] public string? Description { get; set; }
    }
}
