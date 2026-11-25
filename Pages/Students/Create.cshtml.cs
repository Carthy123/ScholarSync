using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScholarSync.Data;
using ScholarSync.Models;
using ScholarSync.Security;

namespace ScholarSync.Pages.Students;

public class CreateModel : PageModel
{
    private readonly SchoolDbContext _context;
    private readonly AuditService _auditService;
    private readonly SchoolAccessService _accessService;

    public CreateModel(SchoolDbContext context, AuditService auditService, SchoolAccessService accessService)
    {
        _context = context;
        _auditService = auditService;
        _accessService = accessService;
    }

    [BindProperty]
    public Student Student { get; set; } = new();

    [BindProperty]
    public IFormFile? PhotoFile { get; set; }

    public IList<SchoolClass> Classes { get; set; } = new List<SchoolClass>();

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await _accessService.CanAccessFeatureAsync("StudentManagement"))
            return Forbid();
        await LoadClassesAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await _accessService.CanAccessFeatureAsync("StudentManagement"))
            return Forbid();
        await ValidatePhotoAsync();
        if (!ModelState.IsValid)
        {
            await LoadClassesAsync();

            return Page();
        }

        if (!_accessService.IsAdmin && (!Student.SchoolClassId.HasValue || !await _accessService.CanAccessClassAsync(Student.SchoolClassId.Value)))
        {
            ModelState.AddModelError("Student.SchoolClassId", "You can only register students in your assigned class.");
            await LoadClassesAsync();
            return Page();
        }

        _context.Students.Add(Student);
        await _context.SaveChangesAsync();
        if (PhotoFile != null && PhotoFile.Length > 0)
        {
            Student.PhotoPath = await SavePhotoAsync(Student.Id, PhotoFile);
            await _context.SaveChangesAsync();
        }
        await _auditService.LogAsync("Created", "Student", Student.Id, $"Added {Student.FirstName} {Student.LastName}.");

        return RedirectToPage("/Students/Index");
    }

    private async Task LoadClassesAsync()
    {
        Classes = ClassOrdering.Order(await _context.Classes.ToListAsync()).ToList();
        if (!_accessService.IsAdmin)
        {
            var allowedClassIds = await _accessService.GetAllowedClassIdsAsync();
            Classes = Classes.Where(schoolClass => allowedClassIds.Contains(schoolClass.Id)).ToList();
        }
    }

    private async Task ValidatePhotoAsync()
    {
        if (PhotoFile == null || PhotoFile.Length == 0)
            return;
        var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".webp" };
        if (!allowedExtensions.Contains(Path.GetExtension(PhotoFile.FileName).ToLowerInvariant()))
            ModelState.AddModelError(nameof(PhotoFile), "Upload a PNG, JPG, JPEG, or WEBP image.");
        if (PhotoFile.Length > 2 * 1024 * 1024)
            ModelState.AddModelError(nameof(PhotoFile), "The student photo must be 2 MB or smaller.");
        await Task.CompletedTask;
    }

    private static async Task<string> SavePhotoAsync(int studentId, IFormFile photoFile)
    {
        var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "StudentPhotos");
        Directory.CreateDirectory(uploadsPath);
        var extension = Path.GetExtension(photoFile.FileName).ToLowerInvariant();
        var fileName = $"student-{studentId}-{Guid.NewGuid():N}{extension}";
        await using var stream = System.IO.File.Create(Path.Combine(uploadsPath, fileName));
        await photoFile.CopyToAsync(stream);
        return fileName;
    }
}
