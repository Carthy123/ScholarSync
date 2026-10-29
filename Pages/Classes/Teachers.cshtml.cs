using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScholarSync.Data;
using ScholarSync.Models;
using ScholarSync.Security;

namespace ScholarSync.Pages.Classes;

[Authorize(Roles = "Admin")]
public class TeachersModel : PageModel
{
    private readonly SchoolDbContext _context;
    private readonly AuditService _auditService;

    public TeachersModel(SchoolDbContext context, AuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public IList<SchoolClass> Classes { get; private set; } = new List<SchoolClass>();
    public IList<AdminUser> Teachers { get; private set; } = new List<AdminUser>();
    public IDictionary<int, ClassTeacher> Assignments { get; private set; } = new Dictionary<int, ClassTeacher>();

    [BindProperty(SupportsGet = true)]
    public string? AcademicYear { get; set; }

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostAssignAsync(int classId, int teacherId)
    {
        var schoolClass = await _context.Classes.FindAsync(classId);
        var teacher = await _context.AdminUsers.FirstOrDefaultAsync(user => user.Id == teacherId && user.Role == "Teacher");
        if (schoolClass == null || teacher == null)
            return NotFound();

        var existing = await _context.ClassTeachers.FirstOrDefaultAsync(assignment => assignment.SchoolClassId == classId);
        if (existing == null)
        {
            _context.ClassTeachers.Add(new ClassTeacher { SchoolClassId = classId, AdminUserId = teacherId });
        }
        else
        {
            existing.AdminUserId = teacherId;
            existing.AssignedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        await _auditService.LogAsync("Assigned", "ClassTeacher", classId, $"Assigned {teacher.Username} to {schoolClass.Name} ({schoolClass.AcademicYear}).");
        return RedirectToPage(new { AcademicYear = schoolClass.AcademicYear });
    }

    public async Task<IActionResult> OnPostRemoveAsync(int classId)
    {
        var assignment = await _context.ClassTeachers.Include(item => item.AdminUser).FirstOrDefaultAsync(item => item.SchoolClassId == classId);
        if (assignment != null)
        {
            _context.ClassTeachers.Remove(assignment);
            await _context.SaveChangesAsync();
            await _auditService.LogAsync("Removed", "ClassTeacher", classId, $"Removed {assignment.AdminUser.Username} from the class.");
        }
        return RedirectToPage(new { AcademicYear });
    }

    private async Task LoadAsync()
    {
        var years = await _context.Classes.AsNoTracking().Select(item => item.AcademicYear).Distinct().OrderByDescending(year => year).ToListAsync();
        AcademicYear ??= years.FirstOrDefault();
        Classes = await _context.Classes.Where(item => item.AcademicYear == AcademicYear).AsNoTracking().OrderBy(item => item.Name).ToListAsync();
        Teachers = await _context.AdminUsers.Where(user => user.Role == "Teacher").AsNoTracking().OrderBy(user => user.Username).ToListAsync();
        var classIds = Classes.Select(item => item.Id).ToList();
        Assignments = await _context.ClassTeachers.Include(item => item.AdminUser).Where(item => classIds.Contains(item.SchoolClassId)).AsNoTracking().ToDictionaryAsync(item => item.SchoolClassId);
    }
}
