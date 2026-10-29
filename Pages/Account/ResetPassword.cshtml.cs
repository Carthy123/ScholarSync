using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScholarSync.Data;
using ScholarSync.Models;
using ScholarSync.Security;

namespace ScholarSync.Pages.Account;

public class ResetPasswordModel : PageModel
{
    private readonly SchoolDbContext _context;

    public ResetPasswordModel(SchoolDbContext context) => _context = context;

    [BindProperty(SupportsGet = true)] public string Token { get; set; } = string.Empty;
    [BindProperty] public string Password { get; set; } = string.Empty;
    [BindProperty] public string ConfirmPassword { get; set; } = string.Empty;
    public bool IsValidToken { get; private set; }

    public async Task OnGetAsync() => IsValidToken = await FindTokenAsync() != null;

    public async Task<IActionResult> OnPostAsync()
    {
        var resetToken = await FindTokenAsync();
        if (resetToken == null)
        {
            ModelState.AddModelError(string.Empty, "This reset link is invalid or has expired.");
            IsValidToken = false;
            return Page();
        }
        if (Password.Length < 8)
            ModelState.AddModelError(nameof(Password), "Password must be at least 8 characters.");
        if (Password != ConfirmPassword)
            ModelState.AddModelError(nameof(ConfirmPassword), "Passwords do not match.");
        if (!ModelState.IsValid)
        {
            IsValidToken = true;
            return Page();
        }

        var (hash, salt) = PasswordService.HashPassword(Password);
        resetToken.AdminUser.PasswordHash = hash;
        resetToken.AdminUser.PasswordSalt = salt;
        resetToken.UsedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return RedirectToPage("/Account/Login");
    }

    private Task<PasswordResetToken?> FindTokenAsync() => _context.PasswordResetTokens
        .Include(reset => reset.AdminUser)
        .FirstOrDefaultAsync(reset => reset.TokenHash == PasswordService.HashToken(Token) && reset.UsedAt == null && reset.ExpiresAt > DateTime.UtcNow);
}
