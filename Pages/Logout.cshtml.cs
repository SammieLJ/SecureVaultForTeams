using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SecureVaultForTeams.Services;

namespace SecureVaultForTeams.Pages;

public class LogoutModel(AuthService authService) : PageModel
{
    public async Task<IActionResult> OnGetAsync()
    {
        await authService.SignOutAsync(HttpContext);
        return RedirectToPage("/Login");
    }
}
