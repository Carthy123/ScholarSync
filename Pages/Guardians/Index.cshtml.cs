using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScholarSync.Data;
using ScholarSync.Models;
using ScholarSync.Security;

namespace ScholarSync.Pages.Guardians;

public class IndexModel : PageModel
{
    private readonly SchoolDbContext _context;
    private readonly SchoolAccessService _accessService;

    public IndexModel(SchoolDbContext context, SchoolAccessService accessService)
    {
        _context = context;
        _accessService = accessService;
    }

    public IList<GuardianRow> Guardians { get; private set; } = new List<GuardianRow>();

    [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    public async Task OnGetAsync()
    {
        var students = await _context.Students
            .Include(student => student.SchoolClass)
            .AsNoTracking()
            .OrderBy(student => student.LastName)
            .ThenBy(student => student.FirstName)
            .ToListAsync();
        if (!_accessService.IsAdmin)
        {
            var allowedClassIds = await _accessService.GetAllowedClassIdsAsync();
            students = students.Where(student => student.SchoolClassId.HasValue && allowedClassIds.Contains(student.SchoolClassId.Value)).ToList();
        }

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var search = SearchTerm.Trim();
            students = students.Where(student =>
                (student.GuardianName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (student.GuardianPhone?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (student.GuardianEmail?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                student.FirstName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                student.LastName.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        Guardians = students
            .GroupBy(student => new
            {
                Name = Normalize(student.GuardianName, "Guardian details not provided"),
                Phone = Normalize(student.GuardianPhone, "—"),
                Email = Normalize(student.GuardianEmail, "—"),
                Address = Normalize(student.GuardianAddress, "—")
            })
            .Select(group => new GuardianRow
            {
                Name = group.Key.Name,
                Phone = group.Key.Phone,
                Email = group.Key.Email,
                Address = group.Key.Address,
                Students = group.Select(student => new StudentSummary
                {
                    Name = $"{student.FirstName} {student.LastName}",
                    ClassName = student.SchoolClass?.Name ?? "Unassigned",
                    EmergencyContact = string.IsNullOrWhiteSpace(student.EmergencyContactName) ? "—" : $"{student.EmergencyContactName} ({student.EmergencyContactPhone ?? "no phone"})"
                }).ToList()
            })
            .OrderBy(guardian => guardian.Name)
            .ToList();
    }

    private static string Normalize(string? value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    public class GuardianRow
    {
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public IList<StudentSummary> Students { get; set; } = new List<StudentSummary>();
    }

    public class StudentSummary
    {
        public string Name { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string EmergencyContact { get; set; } = string.Empty;
    }
}
