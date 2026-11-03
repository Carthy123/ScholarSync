using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ScholarSync.Security;

namespace ScholarSync.Pages.Account;

public class LogoutModel : PageModel
{
    private readonly AuditService _auditService;

    public LogoutModel(AuditService auditService)
    {
        _auditService = auditService;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (User.Identity?.IsAuthenticated == true)
            await _auditService.LogAsync("Logout", "AdminUser", details: "Signed out.");

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage("/Account/Login");
    }
}
