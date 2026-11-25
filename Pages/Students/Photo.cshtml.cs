using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScholarSync.Data;
using ScholarSync.Security;

namespace ScholarSync.Pages.Students;

public class PhotoModel : PageModel
{
    private readonly SchoolDbContext _context;
    private readonly SchoolAccessService _accessService;

    public PhotoModel(SchoolDbContext context, SchoolAccessService accessService)
    {
        _context = context;
        _accessService = accessService;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var student = await _context.Students
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id);
        if (student == null || string.IsNullOrWhiteSpace(student.PhotoPath))
            return NotFound();
        if (!await _accessService.CanAccessStudentAsync(id))
            return Forbid();

        var fileName = Path.GetFileName(student.PhotoPath);
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var contentType = extension switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            _ => string.Empty
        };
        if (string.IsNullOrWhiteSpace(contentType))
            return NotFound();

        var photoPath = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "StudentPhotos", fileName);
        return System.IO.File.Exists(photoPath) ? PhysicalFile(photoPath, contentType) : NotFound();
    }
}
