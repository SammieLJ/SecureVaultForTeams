using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using SecureVaultForTeams.Services;

namespace SecureVaultForTeams.Pages
{
    [AllowAnonymous]
    public class TestLoginModel : PageModel
    {
        private readonly AuthService _authService;
        private readonly DatabaseService _dbService;

        public TestLoginModel(AuthService authService, DatabaseService dbService)
        {
            _authService = authService;
            _dbService = dbService;
        }

        [BindProperty]
        public string Username { get; set; } = string.Empty;

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Try to authenticate
            var success = await _authService.SignInAsync(HttpContext, Username, "false");
            if (!success)
            {
                ModelState.AddModelError(string.Empty, "Invalid username or password");
                return Page();
            }

            return RedirectToPage("/Index");
        }
    }
}
