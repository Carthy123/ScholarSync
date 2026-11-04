using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScholarSync.Data;
using ScholarSync.Models;
using ScholarSync.Security;

namespace ScholarSync.Pages.Account;

public class LoginModel : PageModel
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(10);

    private readonly SchoolDbContext _context;
    private readonly AuditService _auditService;

    public LoginModel(SchoolDbContext context, AuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    [BindProperty]
    public LoginInput Input { get; set; } = new();
    public SchoolSettings Settings { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToPage("/Index");

        if (!await _context.AdminUsers.AnyAsync())
            return RedirectToPage("/Account/Setup");

        await LoadBrandingAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        var username = Input.Username.Trim();
        var user = await _context.AdminUsers.FirstOrDefaultAsync(admin => admin.Username == username);
        var now = DateTime.UtcNow;

        if (user?.LockoutEnd is DateTime lockoutEnd && lockoutEnd > now)
        {
            await _auditService.LogAsync(
                "Blocked login",
                "AdminUser",
                user.Id,
                "Sign-in blocked because the account is temporarily locked.",
                username);
            await LoadBrandingAsync();
            ModelState.AddModelError(string.Empty, "Invalid username or password, or the account is temporarily locked. Please try again later.");
            return Page();
        }

        if (user?.LockoutEnd is not null && user.LockoutEnd <= now)
        {
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            await _context.SaveChangesAsync();
        }

        if (user == null || !PasswordService.VerifyPassword(Input.Password, user.PasswordHash, user.PasswordSalt))
        {
            if (user != null)
            {
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= MaxFailedAttempts)
                    user.LockoutEnd = now.Add(LockoutDuration);
                await _context.SaveChangesAsync();
            }

            await _auditService.LogAsync(
                "Failed login",
                "AdminUser",
                user?.Id,
                $"Rejected sign-in attempt for username '{username}'.",
                string.IsNullOrWhiteSpace(username) ? "Unknown" : username);
            await LoadBrandingAsync();
            ModelState.AddModelError(string.Empty, "Invalid username or password, or the account is temporarily locked.");
            return Page();
        }

        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        await _context.SaveChangesAsync();

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        await _auditService.LogAsync("Login", "AdminUser", user.Id, $"Signed in as {user.Role}.");

        return LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl);
    }

    private async Task LoadBrandingAsync()
    {
        Settings = await _context.SchoolSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(settings => settings.Id == 1) ?? new SchoolSettings();
    }

    public class LoginInput
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
