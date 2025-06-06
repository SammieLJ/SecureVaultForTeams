using LiteDB;

namespace SecureVaultForTeams.Models;

public class Entry
{
    [BsonId]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public string TeamId { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public EntryType Type { get; set; } = EntryType.Password;    public DateTime Created { get; set; }
    public DateTime LastModified { get; set; }
    public bool Deleted { get; set; } = false;
    public string? TeamName { get; set; } = "No team assigned!";

    public void Sanitize()
    {
        Title = Title ?? string.Empty;
        Username = Username ?? string.Empty;
        Password = Password ?? string.Empty;
        Url = Url ?? string.Empty;
        Notes = Notes ?? string.Empty;
        TeamId = TeamId ?? string.Empty;
    }
}

public enum EntryType
{
    Password,
    SecureNote
}
