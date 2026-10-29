using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScholarSync.Data;
using ScholarSync.Models;
using ScholarSync.Security;

namespace ScholarSync.Pages.Account;

public class ForgotPasswordModel : PageModel
{
    private readonly SchoolDbContext _context;
    private readonly EmailSender _emailSender;

    public ForgotPasswordModel(SchoolDbContext context, EmailSender emailSender)
    {
        _context = context;
        _emailSender = emailSender;
    }

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    public bool Submitted { get; private set; }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            ModelState.AddModelError(nameof(Email), "Enter your email address.");
            return Page();
        }

        var user = await _context.AdminUsers.FirstOrDefaultAsync(admin => admin.Email == Email.Trim());
        if (user != null)
        {
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .Replace("+", "-").Replace("/", "_").TrimEnd('=');
            _context.PasswordResetTokens.Add(new PasswordResetToken
            {
                AdminUserId = user.Id,
                TokenHash = PasswordService.HashToken(token),
                ExpiresAt = DateTime.UtcNow.AddMinutes(30)
            });
            await _context.SaveChangesAsync();

            var link = Url.Page("/Account/ResetPassword", null, new { token }, Request.Scheme, Request.Host.Value);
            await _emailSender.SendPasswordResetAsync(user.Email, link!);
        }

        // Do not reveal whether the email exists in the system.
        Submitted = true;
        return Page();
    }
}
