using Microsoft.EntityFrameworkCore;
using ScholarSync.Data;

namespace ScholarSync.Security;

public class AcademicYearService
{
    private readonly SchoolDbContext _context;

    public AcademicYearService(SchoolDbContext context) => _context = context;

    public async Task<string> GetCurrentAsync()
    {
        var year = await _context.SchoolSettings
            .AsNoTracking()
            .Where(settings => settings.Id == 1)
            .Select(settings => settings.AcademicYear)
            .FirstOrDefaultAsync();

        return string.IsNullOrWhiteSpace(year) ? DateTime.UtcNow.Year.ToString() : year;
    }
}
