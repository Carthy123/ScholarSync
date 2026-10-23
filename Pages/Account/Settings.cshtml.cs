using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScholarSync.Data;
using ScholarSync.Models;

namespace ScholarSync.Pages.Account;

[Authorize(Roles = "Admin")]
public class SettingsModel : PageModel
{
    private readonly SchoolDbContext _context;

    public SettingsModel(SchoolDbContext context) => _context = context;

    [BindProperty]
    public SchoolSettings Settings { get; set; } = new();

    public async Task OnGetAsync() => await LoadSettingsAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        Settings.Id = 1;
        if (!ModelState.IsValid)
            return Page();

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
            existing.PrimaryColor = Settings.PrimaryColor.Trim().ToUpperInvariant();
            existing.AccentColor = Settings.AccentColor.Trim().ToUpperInvariant();
        }

        await _context.SaveChangesAsync();
        TempData["SettingsSaved"] = "School branding has been updated.";
        return RedirectToPage();
    }

    private async Task LoadSettingsAsync()
    {
        Settings = await _context.SchoolSettings.FirstOrDefaultAsync(settings => settings.Id == 1) ?? new SchoolSettings();
    }
}
