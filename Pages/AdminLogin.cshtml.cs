using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using SecureVaultForTeams.Services;

namespace SecureVaultForTeams.Pages
{
    [AllowAnonymous]
    public class AdminLoginModel : PageModel
    {
        private readonly AuthService _authService;
        private readonly DatabaseService _dbService;

        public AdminLoginModel(AuthService authService, DatabaseService dbService)
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

            // Check if this is an admin user
            var user = _dbService.GetUserByUsername(Username);
            if (user == null || user.Role != "Admin" || !BCrypt.Net.BCrypt.Verify(Password, user.PasswordHash))
            {
                ModelState.AddModelError(string.Empty, "Invalid admin username or password");
                return Page();
            }

            // Authenticate as admin
            var success = await _authService.SignInAsync(HttpContext, Username, "true");
            if (!success)
            {
                ModelState.AddModelError(string.Empty, "Authentication failed");
                return Page();
            }

            return RedirectToPage("/Index");
        }
    }
}
