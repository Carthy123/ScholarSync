using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScholarSync.Data;
using ScholarSync.Models;
using ScholarSync.Security;
using System.ComponentModel.DataAnnotations;

namespace ScholarSync.Pages.Account;

[Authorize(Roles = "Admin")]
public class UsersModel : PageModel
{
    private readonly SchoolDbContext _context;
    private readonly AuditService _auditService;

    public UsersModel(SchoolDbContext context, AuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    [BindProperty]
    public NewAdminInput Input { get; set; } = new();

    public IList<AdminUser> Admins { get; private set; } = new List<AdminUser>();

    public async Task OnGetAsync() => await LoadAdminsAsync();

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (Input.Password.Length < 8)
            ModelState.AddModelError("Input.Password", "Password must be at least 8 characters.");
        if (string.IsNullOrWhiteSpace(Input.Username))
            ModelState.AddModelError("Input.Username", "Username is required.");
        if (Input.Role is not ("Admin" or "Teacher"))
            ModelState.AddModelError("Input.Role", "Select a valid account role.");
        if (!new EmailAddressAttribute().IsValid(Input.Email))
            ModelState.AddModelError("Input.Email", "Enter a valid email address.");
        if (await _context.AdminUsers.AnyAsync(admin => admin.Username == Input.Username.Trim()))
            ModelState.AddModelError("Input.Username", "That username already exists.");

        if (!ModelState.IsValid)
        {
            await LoadAdminsAsync();
            return Page();
        }

        var (hash, salt) = PasswordService.HashPassword(Input.Password);
        var admin = new AdminUser { Username = Input.Username.Trim(), Email = Input.Email.Trim(), Role = Input.Role, PasswordHash = hash, PasswordSalt = salt };
        _context.AdminUsers.Add(admin);
        await _context.SaveChangesAsync();
        await _auditService.LogAsync("Created", "AdminUser", admin.Id, $"Created {admin.Role} account '{admin.Username}'.");
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var currentId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (id == currentId)
        {
            TempData["AccountError"] = "You cannot delete the account you are currently using.";
            return RedirectToPage();
        }

        var admin = await _context.AdminUsers.FindAsync(id);
        if (admin != null)
        {
            _context.AdminUsers.Remove(admin);
            await _context.SaveChangesAsync();
            await _auditService.LogAsync("Deleted", "AdminUser", id, $"Deleted account '{admin.Username}'.");
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateEmailAsync(int id, string email)
    {
        if (!new EmailAddressAttribute().IsValid(email))
        {
            TempData["AccountError"] = "Enter a valid email address.";
            return RedirectToPage();
        }

        var admin = await _context.AdminUsers.FindAsync(id);
        if (admin != null)
        {
            admin.Email = email.Trim();
            await _context.SaveChangesAsync();
            await _auditService.LogAsync("Updated", "AdminUser", id, $"Updated email for '{admin.Username}'.");
        }
        return RedirectToPage();
    }

    private async Task LoadAdminsAsync() => Admins = await _context.AdminUsers.AsNoTracking().OrderBy(admin => admin.Username).ToListAsync();

    public class NewAdminInput
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = "Teacher";
        public string Password { get; set; } = string.Empty;
    }
}
