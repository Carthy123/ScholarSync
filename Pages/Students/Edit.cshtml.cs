using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScholarSync.Data;
using ScholarSync.Models;
using ScholarSync.Security;

namespace ScholarSync.Pages.Students;

public class EditModel : PageModel
{
    private readonly SchoolDbContext _context;
    private readonly AuditService _auditService;
    private readonly SchoolAccessService _accessService;

    public EditModel(SchoolDbContext context, AuditService auditService, SchoolAccessService accessService)
    {
        _context = context;
        _auditService = auditService;
        _accessService = accessService;
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
        if (!await _accessService.CanAccessStudentAsync(id))
            return Forbid();

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
        if (!await _accessService.CanAccessStudentAsync(Student.Id) || (!_accessService.IsAdmin && (!Student.SchoolClassId.HasValue || !await _accessService.CanAccessClassAsync(Student.SchoolClassId.Value))))
            return Forbid();
        await _context.SaveChangesAsync();
        await _auditService.LogAsync("Updated", "Student", Student.Id, $"Updated {Student.FirstName} {Student.LastName}.");

        return RedirectToPage("/Students/Index");
    }

    private async Task LoadClassesAsync()
    {
        Classes = await _context.Classes
            .OrderBy(schoolClass => schoolClass.Name)
            .ToListAsync();
        if (!_accessService.IsAdmin)
        {
            var allowedClassIds = await _accessService.GetAllowedClassIdsAsync();
            Classes = Classes.Where(schoolClass => allowedClassIds.Contains(schoolClass.Id)).ToList();
        }
    }
}
