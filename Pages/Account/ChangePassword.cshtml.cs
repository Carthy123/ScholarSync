using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScholarSync.Data;
using ScholarSync.Security;

namespace ScholarSync.Pages.Account;

[Authorize]
public class ChangePasswordModel : PageModel
{
    private readonly SchoolDbContext _context;
    private readonly AuditService _auditService;

    public ChangePasswordModel(SchoolDbContext context, AuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    [BindProperty]
    public PasswordInput Input { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Input.NewPassword != Input.ConfirmPassword)
            ModelState.AddModelError("Input.ConfirmPassword", "The passwords do not match.");

        if (!ModelState.IsValid)
            return Page();

        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Challenge();

        var user = await _context.AdminUsers.FirstOrDefaultAsync(admin => admin.Id == userId);
        if (user == null)
            return Challenge();

        if (!PasswordService.VerifyPassword(Input.CurrentPassword, user.PasswordHash, user.PasswordSalt))
        {
            ModelState.AddModelError("Input.CurrentPassword", "The current password is incorrect.");
            return Page();
        }

        var (hash, salt) = PasswordService.HashPassword(Input.NewPassword);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;
        await _context.SaveChangesAsync();
        await _auditService.LogAsync("Updated", "AdminUser", user.Id, "Changed the account password.");

        TempData["PasswordChanged"] = "Your password has been changed successfully.";
        return RedirectToPage();
    }

    public class PasswordInput
    {
        [Required, DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required, MinLength(8, ErrorMessage = "The new password must be at least 8 characters."), DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
