using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScholarSync.Data;
using ScholarSync.Models;
using ScholarSync.Security;

namespace ScholarSync.Pages.Subjects;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly SchoolDbContext _context;
    private readonly AuditService _auditService;

    public IndexModel(SchoolDbContext context, AuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public IList<SchoolSubject> Subjects { get; private set; } = new List<SchoolSubject>();

    [BindProperty]
    public SchoolSubject Input { get; set; } = new();

    public IReadOnlyList<string> LevelOptions { get; } = new[]
    {
        "All levels", "Creche", "KG", "Primary", "JHS", "KG / Primary", "Primary / JHS"
    };

    public async Task OnGetAsync() => await LoadSubjectsAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        Input.Name = Input.Name.Trim();
        Input.Level = Input.Level.Trim();
        if (!LevelOptions.Contains(Input.Level, StringComparer.OrdinalIgnoreCase))
            ModelState.AddModelError("Input.Level", "Select a valid class level.");

        var duplicate = await _context.SchoolSubjects.AnyAsync(subject =>
            subject.Name.ToLower() == Input.Name.ToLower() && subject.Level.ToLower() == Input.Level.ToLower());
        if (duplicate)
            ModelState.AddModelError("Input.Name", "That subject is already listed for this level.");

        if (!ModelState.IsValid)
        {
            await LoadSubjectsAsync();
            return Page();
        }

        var subject = new SchoolSubject { Name = Input.Name, Level = Input.Level, IsActive = true };
        _context.SchoolSubjects.Add(subject);
        await _context.SaveChangesAsync();
        await _auditService.LogAsync("Created", "SchoolSubject", subject.Id, $"Added subject {subject.Name} for {subject.Level}.");
        TempData["SubjectMessage"] = $"{subject.Name} was added to the subject list.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var subject = await _context.SchoolSubjects.FirstOrDefaultAsync(item => item.Id == id);
        if (subject == null)
            return NotFound();

        subject.IsActive = !subject.IsActive;
        await _context.SaveChangesAsync();
        await _auditService.LogAsync(subject.IsActive ? "Activated" : "Archived", "SchoolSubject", subject.Id, $"{(subject.IsActive ? "Activated" : "Archived")} subject {subject.Name}.");
        TempData["SubjectMessage"] = subject.IsActive ? $"{subject.Name} is active again." : $"{subject.Name} was archived.";
        return RedirectToPage();
    }

    private async Task LoadSubjectsAsync()
    {
        Subjects = await _context.SchoolSubjects
            .AsNoTracking()
            .OrderByDescending(subject => subject.IsActive)
            .ThenBy(subject => subject.Level)
            .ThenBy(subject => subject.Name)
            .ToListAsync();
    }
}
