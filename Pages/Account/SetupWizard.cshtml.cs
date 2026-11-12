using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScholarSync.Data;
using ScholarSync.Models;
using ScholarSync.Security;

namespace ScholarSync.Pages.Account;

[Authorize(Roles = "Admin")]
public class SetupWizardModel : PageModel
{
    private readonly SchoolDbContext _context;
    private readonly AuditService _auditService;

    public SetupWizardModel(SchoolDbContext context, AuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    [BindProperty]
    public SchoolSettings Settings { get; set; } = new();

    [BindProperty]
    public IFormFile? CrestFile { get; set; }

    public int ClassCount { get; private set; }
    public int AccountCount { get; private set; }
    public bool HasCrest => !string.IsNullOrWhiteSpace(Settings.LogoPath);

    public async Task OnGetAsync() => await LoadAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        Settings.Id = 1;
        if (!ModelState.IsValid)
        {
            await LoadReadinessAsync();
            return Page();
        }

        if (CrestFile != null && CrestFile.Length > 0)
        {
            var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".webp" };
            var extension = Path.GetExtension(CrestFile.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
                ModelState.AddModelError(nameof(CrestFile), "Upload a PNG, JPG, JPEG, or WEBP image.");
            else if (CrestFile.Length > 2 * 1024 * 1024)
                ModelState.AddModelError(nameof(CrestFile), "The crest image must be 2 MB or smaller.");
            else
            {
                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                Directory.CreateDirectory(uploadsPath);
                var fileName = $"school-crest{extension}";
                await using var stream = System.IO.File.Create(Path.Combine(uploadsPath, fileName));
                await CrestFile.CopyToAsync(stream);
                Settings.LogoPath = $"/uploads/{fileName}";
            }
        }
        if (!ModelState.IsValid)
        {
            await LoadReadinessAsync();
            return Page();
        }

        var existing = await _context.SchoolSettings.FirstOrDefaultAsync(item => item.Id == 1);
        if (existing == null) _context.SchoolSettings.Add(Settings);
        else
        {
            existing.SchoolName = Settings.SchoolName.Trim(); existing.AcademicYear = Settings.AcademicYear.Trim(); existing.WelcomeMessage = Settings.WelcomeMessage.Trim();
            existing.SchoolAddress = Settings.SchoolAddress.Trim(); existing.SchoolPhone = Settings.SchoolPhone.Trim(); existing.SchoolEmail = Settings.SchoolEmail.Trim();
            existing.PrimaryColor = Settings.PrimaryColor.Trim().ToUpperInvariant(); existing.AccentColor = Settings.AccentColor.Trim().ToUpperInvariant(); existing.LogoPath = Settings.LogoPath;
        }
        await _context.SaveChangesAsync();
        await _auditService.LogAsync("Updated", "SchoolSettings", 1, $"Completed setup wizard for {Settings.SchoolName}.");
        TempData["WizardSaved"] = "School setup has been saved.";
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        Settings = await _context.SchoolSettings.FirstOrDefaultAsync(item => item.Id == 1) ?? new SchoolSettings();
        await LoadReadinessAsync();
    }

    private async Task LoadReadinessAsync()
    {
        ClassCount = await _context.Classes.CountAsync();
        AccountCount = await _context.AdminUsers.CountAsync();
    }
}
