using LiteDB;
using System;

namespace SecureVaultForTeams.Models;

public class Team
{
    [BsonId]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> MemberIds { get; set; } = new();
    public DateTime Created { get; set; } = DateTime.Now;
}
