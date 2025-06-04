using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using SecureVaultForTeams.Models;

namespace SecureVaultForTeams.Services;

public class AuthService
{
    private readonly DatabaseService _db;

    public AuthService(DatabaseService db)
    {
        _db = db;
    }    public async Task<bool> SignInAsync(HttpContext context, string username, string password)
    {
        var user = _db.GetUserByUsername(username);
        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            return false;
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role ?? string.Empty),
            new("UserId", user.Id)
        };

        // For admin users, always add access to all teams
        if ((user.Role ?? string.Empty) == "Admin")
        {
            var allTeams = _db.GetTeams();
            foreach (var team in allTeams)
            {
                claims.Add(new Claim("TeamId", team.Id));
            }
        }
        else
        {
            if (user.TeamIds != null)
            {
                foreach (var teamId in user.TeamIds)
                {
                    claims.Add(new Claim("TeamId", teamId));
                }
            }
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        return true;
    }

    public async Task SignOutAsync(HttpContext context)
    {
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    public string CreateUser(string username, string password, string role = "User")
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
        var user = new User
        {
            Username = username,
            PasswordHash = passwordHash,
            Role = role
        };

        _db.CreateUser(user);
        return user.Id;
    }
}
