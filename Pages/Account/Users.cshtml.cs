using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScholarSync.Data;
using ScholarSync.Models;
using ScholarSync.Security;

namespace ScholarSync.Pages.Account;

[Authorize(Roles = "Admin")]
public class UsersModel : PageModel
{
    private readonly SchoolDbContext _context;

    public UsersModel(SchoolDbContext context) => _context = context;

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
        if (await _context.AdminUsers.AnyAsync(admin => admin.Username == Input.Username.Trim()))
            ModelState.AddModelError("Input.Username", "That username already exists.");

        if (!ModelState.IsValid)
        {
            await LoadAdminsAsync();
            return Page();
        }

        var (hash, salt) = PasswordService.HashPassword(Input.Password);
        _context.AdminUsers.Add(new AdminUser { Username = Input.Username.Trim(), PasswordHash = hash, PasswordSalt = salt });
        await _context.SaveChangesAsync();
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
        }
        return RedirectToPage();
    }

    private async Task LoadAdminsAsync() => Admins = await _context.AdminUsers.AsNoTracking().OrderBy(admin => admin.Username).ToListAsync();

    public class NewAdminInput
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
