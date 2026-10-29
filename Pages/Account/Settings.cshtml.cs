using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScholarSync.Data;
using ScholarSync.Models;
using ScholarSync.Security;

namespace ScholarSync.Pages.Account;

[Authorize(Roles = "Admin")]
public class SettingsModel : PageModel
{
    private readonly SchoolDbContext _context;
    private readonly AuditService _auditService;

    public SettingsModel(SchoolDbContext context, AuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    [BindProperty]
    public SchoolSettings Settings { get; set; } = new();

    [BindProperty]
    public IFormFile? CrestFile { get; set; }

    public async Task OnGetAsync() => await LoadSettingsAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        Settings.Id = 1;
        if (!ModelState.IsValid)
            return Page();

        if (CrestFile != null && CrestFile.Length > 0)
        {
            var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".webp" };
            var extension = Path.GetExtension(CrestFile.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                ModelState.AddModelError(nameof(CrestFile), "Upload a PNG, JPG, JPEG, or WEBP image.");
                return Page();
            }

            if (CrestFile.Length > 2 * 1024 * 1024)
            {
                ModelState.AddModelError(nameof(CrestFile), "The crest image must be 2 MB or smaller.");
                return Page();
            }

            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            Directory.CreateDirectory(uploadsPath);
            var fileName = $"school-crest{extension}";
            var physicalPath = Path.Combine(uploadsPath, fileName);
            await using var stream = System.IO.File.Create(physicalPath);
            await CrestFile.CopyToAsync(stream);
            Settings.LogoPath = $"/uploads/{fileName}";
        }

        var existing = await _context.SchoolSettings.FirstOrDefaultAsync(settings => settings.Id == 1);
        if (existing == null)
        {
            _context.SchoolSettings.Add(Settings);
        }
        else
        {
            existing.SchoolName = Settings.SchoolName.Trim();
            existing.AcademicYear = Settings.AcademicYear.Trim();
            existing.WelcomeMessage = Settings.WelcomeMessage.Trim();
            existing.SchoolAddress = Settings.SchoolAddress.Trim();
            existing.SchoolPhone = Settings.SchoolPhone.Trim();
            existing.SchoolEmail = Settings.SchoolEmail.Trim();
            existing.PrimaryColor = Settings.PrimaryColor.Trim().ToUpperInvariant();
            existing.AccentColor = Settings.AccentColor.Trim().ToUpperInvariant();
            existing.LogoPath = Settings.LogoPath;
        }

        await _context.SaveChangesAsync();
        await _auditService.LogAsync("Updated", "SchoolSettings", 1, $"Updated branding for {Settings.SchoolName}.");
        TempData["SettingsSaved"] = "School branding has been updated.";
        return RedirectToPage();
    }

    private async Task LoadSettingsAsync()
    {
        Settings = await _context.SchoolSettings.FirstOrDefaultAsync(settings => settings.Id == 1) ?? new SchoolSettings();
    }
}
