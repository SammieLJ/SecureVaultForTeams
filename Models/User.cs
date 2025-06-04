using LiteDB;

namespace SecureVaultForTeams.Models;

public class User
{
    [BsonId]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "User"; // Admin or User
    public List<string> TeamIds { get; set; } = new();
}
