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

    [BindProperty]
    public string DashboardPreset { get; set; } = "custom";

    public async Task OnGetAsync()
    {
        await LoadSettingsAsync();
        DashboardPreset = InferPreset(Settings);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Settings.Id = 1;
        ApplyPreset(Settings, DashboardPreset);
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
            existing.ShowAnnouncements = Settings.ShowAnnouncements;
            existing.ShowUpcomingActivities = Settings.ShowUpcomingActivities;
            existing.ShowNeedsAttention = Settings.ShowNeedsAttention;
            existing.ShowTeacherWorkload = Settings.ShowTeacherWorkload;
            existing.ShowAnalytics = Settings.ShowAnalytics;
            existing.ShowRecentStudents = Settings.ShowRecentStudents;
            existing.ShowQuickActions = Settings.ShowQuickActions;
            existing.ShowRecentGrades = Settings.ShowRecentGrades;
            existing.ShowClassEnrollment = Settings.ShowClassEnrollment;
            existing.ShowAttendanceOverview = Settings.ShowAttendanceOverview;
            existing.ShowStudentQuickAction = Settings.ShowStudentQuickAction;
            existing.ShowClassQuickAction = Settings.ShowClassQuickAction;
            existing.ShowAttendanceQuickAction = Settings.ShowAttendanceQuickAction;
            existing.ShowGradesQuickAction = Settings.ShowGradesQuickAction;
            existing.ShowFeesQuickAction = Settings.ShowFeesQuickAction;
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

    private static string InferPreset(SchoolSettings settings)
    {
        if (settings.ShowAnnouncements && settings.ShowUpcomingActivities && settings.ShowNeedsAttention && settings.ShowTeacherWorkload && settings.ShowAnalytics && settings.ShowRecentStudents && settings.ShowQuickActions && settings.ShowRecentGrades && settings.ShowClassEnrollment && settings.ShowAttendanceOverview && settings.ShowStudentQuickAction && settings.ShowClassQuickAction && settings.ShowAttendanceQuickAction && settings.ShowGradesQuickAction && settings.ShowFeesQuickAction)
            return "full";

        if (settings.ShowUpcomingActivities && settings.ShowNeedsAttention && settings.ShowAnalytics && settings.ShowRecentStudents && settings.ShowQuickActions && settings.ShowAttendanceOverview && !settings.ShowRecentGrades && !settings.ShowClassEnrollment)
            return "operations";

        if (settings.ShowAnnouncements && settings.ShowUpcomingActivities && settings.ShowAnalytics && settings.ShowRecentGrades && settings.ShowClassEnrollment && settings.ShowAttendanceOverview && !settings.ShowRecentStudents)
            return "academic";

        return "custom";
    }

    private static void ApplyPreset(SchoolSettings settings, string? preset)
    {
        if (string.Equals(preset, "full", StringComparison.OrdinalIgnoreCase))
        {
            settings.ShowAnnouncements = true; settings.ShowUpcomingActivities = true; settings.ShowNeedsAttention = true; settings.ShowTeacherWorkload = true; settings.ShowAnalytics = true; settings.ShowRecentStudents = true; settings.ShowQuickActions = true; settings.ShowRecentGrades = true; settings.ShowClassEnrollment = true; settings.ShowAttendanceOverview = true;
            settings.ShowStudentQuickAction = true; settings.ShowClassQuickAction = true; settings.ShowAttendanceQuickAction = true; settings.ShowGradesQuickAction = true; settings.ShowFeesQuickAction = true;
        }
        else if (string.Equals(preset, "operations", StringComparison.OrdinalIgnoreCase))
        {
            settings.ShowAnnouncements = true; settings.ShowUpcomingActivities = true; settings.ShowNeedsAttention = true; settings.ShowTeacherWorkload = true; settings.ShowAnalytics = true; settings.ShowRecentStudents = true; settings.ShowQuickActions = true; settings.ShowRecentGrades = false; settings.ShowClassEnrollment = false; settings.ShowAttendanceOverview = true;
            settings.ShowStudentQuickAction = true; settings.ShowClassQuickAction = true; settings.ShowAttendanceQuickAction = true; settings.ShowGradesQuickAction = false; settings.ShowFeesQuickAction = true;
        }
        else if (string.Equals(preset, "academic", StringComparison.OrdinalIgnoreCase))
        {
            settings.ShowAnnouncements = true; settings.ShowUpcomingActivities = true; settings.ShowNeedsAttention = true; settings.ShowTeacherWorkload = true; settings.ShowAnalytics = true; settings.ShowRecentStudents = false; settings.ShowQuickActions = true; settings.ShowRecentGrades = true; settings.ShowClassEnrollment = true; settings.ShowAttendanceOverview = true;
            settings.ShowStudentQuickAction = false; settings.ShowClassQuickAction = true; settings.ShowAttendanceQuickAction = true; settings.ShowGradesQuickAction = true; settings.ShowFeesQuickAction = false;
        }
    }
}
