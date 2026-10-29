using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ScholarSync.Data;

namespace ScholarSync.Security;

public class SchoolAccessService
{
    private readonly SchoolDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SchoolAccessService(SchoolDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsAdmin => _httpContextAccessor.HttpContext?.User.IsInRole("Admin") == true;

    public async Task<List<int>> GetAllowedClassIdsAsync()
    {
        if (IsAdmin)
            return await _context.Classes.Select(schoolClass => schoolClass.Id).ToListAsync();

        if (!int.TryParse(_httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var teacherId))
            return new List<int>();

        return await _context.ClassTeachers
            .Where(assignment => assignment.AdminUserId == teacherId)
            .Select(assignment => assignment.SchoolClassId)
            .ToListAsync();
    }

    public async Task<bool> CanAccessStudentAsync(int studentId)
    {
        if (IsAdmin)
            return true;
        var classIds = await GetAllowedClassIdsAsync();
        return await _context.Students.AnyAsync(student => student.Id == studentId && student.SchoolClassId.HasValue && classIds.Contains(student.SchoolClassId.Value));
    }

    public async Task<bool> CanAccessClassAsync(int classId)
    {
        if (IsAdmin)
            return true;
        var classIds = await GetAllowedClassIdsAsync();
        return classIds.Contains(classId);
    }
}
