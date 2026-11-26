using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScholarSync.Data;
using ScholarSync.Models;
using ScholarSync.Security;

namespace ScholarSync.Pages.Subjects;

[Authorize(Roles = "Admin")]
public class AssignmentsModel : PageModel
{
    private readonly SchoolDbContext _context;
    private readonly AuditService _auditService;

    public AssignmentsModel(SchoolDbContext context, AuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public IList<SchoolClass> Classes { get; private set; } = new List<SchoolClass>();
    public IList<SchoolSubject> Subjects { get; private set; } = new List<SchoolSubject>();

    [BindProperty(SupportsGet = true)]
    public int ClassId { get; set; }

    [BindProperty]
    public List<int> SubjectIds { get; set; } = new();

    public async Task OnGetAsync() => await LoadAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        var schoolClass = await _context.Classes.FirstOrDefaultAsync(item => item.Id == ClassId);
        if (schoolClass == null)
            return NotFound();

        var validSubjectIds = await _context.SchoolSubjects
            .Where(subject => subject.IsActive)
            .Select(subject => subject.Id)
            .ToListAsync();
        var selectedIds = SubjectIds.Intersect(validSubjectIds).Distinct().ToList();
        var existing = await _context.ClassSubjectAssignments.Where(item => item.SchoolClassId == ClassId).ToListAsync();
        _context.ClassSubjectAssignments.RemoveRange(existing);
        _context.ClassSubjectAssignments.AddRange(selectedIds.Select(subjectId => new ClassSubjectAssignment
        {
            SchoolClassId = ClassId,
            SchoolSubjectId = subjectId
        }));
        await _context.SaveChangesAsync();
        await _auditService.LogAsync("Updated", "ClassSubjects", ClassId, $"Updated subjects for {schoolClass.Name}: {selectedIds.Count} assigned.");
        TempData["AssignmentMessage"] = $"Subjects for {schoolClass.Name} have been saved.";
        return RedirectToPage(new { ClassId });
    }

    public bool IsAssigned(int subjectId) => SubjectIds.Contains(subjectId);

    private async Task LoadAsync()
    {
        Classes = await _context.Classes.AsNoTracking().OrderBy(item => item.AcademicYear).ThenBy(item => item.Name).ToListAsync();
        Subjects = await _context.SchoolSubjects.AsNoTracking().Where(item => item.IsActive).OrderBy(item => item.Level).ThenBy(item => item.Name).ToListAsync();
        if (ClassId == 0)
            ClassId = Classes.FirstOrDefault()?.Id ?? 0;
        SubjectIds = await _context.ClassSubjectAssignments
            .Where(item => item.SchoolClassId == ClassId)
            .Select(item => item.SchoolSubjectId)
            .ToListAsync();
    }
}
