using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScholarSync.Data;
using ScholarSync.Models;
using ScholarSync.Security;

namespace ScholarSync.Pages.Account;

public class SetupModel : PageModel
{
    private readonly SchoolDbContext _context;

    public SetupModel(SchoolDbContext context) => _context = context;

    [BindProperty]
    public SetupInput Input { get; set; } = new();

    public bool HasExistingAccount { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        HasExistingAccount = await _context.AdminUsers.AnyAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (await _context.AdminUsers.AnyAsync())
            return RedirectToPage("/Account/Login");

        if (Input.Password.Length < 8)
            ModelState.AddModelError("Input.Password", "Password must be at least 8 characters.");
        if (Input.Password != Input.ConfirmPassword)
            ModelState.AddModelError("Input.ConfirmPassword", "Passwords do not match.");
        if (string.IsNullOrWhiteSpace(Input.Username))
            ModelState.AddModelError("Input.Username", "Username is required.");

        if (!ModelState.IsValid)
            return Page();

        var (hash, salt) = PasswordService.HashPassword(Input.Password);
        _context.AdminUsers.Add(new AdminUser
        {
            Username = Input.Username.Trim(),
            PasswordHash = hash,
            PasswordSalt = salt
        });
        await _context.SaveChangesAsync();
        return RedirectToPage("/Account/Login");
    }

    public class SetupInput
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
