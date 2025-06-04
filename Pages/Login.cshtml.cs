using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using SecureVaultForTeams.Services;

namespace SecureVaultForTeams.Pages;

[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly AuthService _authService;
    private readonly DatabaseService _dbService;

    public LoginModel(AuthService authService, DatabaseService dbService)
    {
        _authService = authService;
        _dbService = dbService;
    }

    [BindProperty]
    public string Username { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    [BindProperty]
    public string TeamId { get; set; } = string.Empty;

    [BindProperty]
    public bool RememberMe { get; set; }
    
    public List<Models.Team> Teams { get; set; } = new();

    public void OnGet()
    {
        Teams = _dbService.GetTeams();
    }    
    
    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        // First check if this is an admin user attempt
        var user = _dbService.GetUserByUsername(Username);
        var isAdmin = user != null && user.Role == "Admin";
    
        // For admin users, we'll ignore some ModelState validations related to TeamId
        if (isAdmin && !string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Password))
        {
            // Continue processing for admin users even if TeamId is empty
            if (ModelState.ContainsKey("TeamId"))
            {
                ModelState.Remove("TeamId");
            }
        }
        else if (!ModelState.IsValid)
        {
            Teams = _dbService.GetTeams();
            return Page();
        }
    
        // Recheck user in case we skipped the earlier validation
        if (user == null || !BCrypt.Net.BCrypt.Verify(Password, user.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password");
            Teams = _dbService.GetTeams();
            return Page();
        }

        // Only require team selection and membership for non-admin users
        if (!isAdmin)
        {
            if (string.IsNullOrEmpty(TeamId))
            {
                ModelState.AddModelError(string.Empty, "Please select a team.");
                Teams = _dbService.GetTeams();
                return Page();
            }
            if (!user.TeamIds.Contains(TeamId))
            {
                ModelState.AddModelError(string.Empty, "You are not a member of the selected team.");
                Teams = _dbService.GetTeams();
                return Page();
            }
        }        // For admin users, we don't need to validate team selection
        var success = await _authService.SignInAsync(HttpContext, Username, Password);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, "Authentication failed");
            Teams = _dbService.GetTeams();
            return Page();
        }

        // Redirect to returnUrl if present, otherwise to Index
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToPage("/Index");
    }
}
