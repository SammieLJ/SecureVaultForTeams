using LiteDB;
using SecureVaultForTeams.Models;

namespace SecureVaultForTeams.Services;

public class DatabaseService
{
    private readonly ILiteDatabase _db;
    private readonly ILiteCollection<User> _users;
    private readonly ILiteCollection<Team> _teams;
    private readonly ILiteCollection<Entry> _entries;

    public DatabaseService(string connectionString = "Filename=Data/SecureVault.db")
    {
        _db = new LiteDatabase(connectionString);
        _users = _db.GetCollection<User>("users");
        _teams = _db.GetCollection<Team>("teams");
        _entries = _db.GetCollection<Entry>("entries");

        // Create indexes
        _users.EnsureIndex(x => x.Username, true);
        _teams.EnsureIndex(x => x.Name, true);
        _entries.EnsureIndex(x => x.TeamId);
    }

    // User methods
    public User? GetUserByUsername(string username) =>
        _users.FindOne(x => x.Username == username);

    public User CreateUser(User user)
    {
        _users.Insert(user);
        return user;
    }

    public int GetTeamMemberCount(string teamId)
    {
        if (string.IsNullOrEmpty(teamId))
            return 0;
            
        var users = GetAllUsers();
        return users.Count(u => u.TeamIds.Contains(teamId));
    }
    
    public List<User> GetAllUsers() =>
        _users.FindAll().ToList();
        
    
    // Team methods
    public List<Team> GetTeams() =>
        _teams.FindAll().ToList();

    public Team? GetTeamById(string id) =>
        _teams.FindById(id);

    public Team CreateTeam(Team team)
    {
        _teams.Insert(team);
        return team;
    }
    
    public bool UpdateTeam(Team team)
    {
        return _teams.Update(team);
    }
    
    public bool DeleteTeam(string id)
    {
        return _teams.Delete(id);
    }
    
    public List<Team> GetAllTeams() => _teams.FindAll().ToList();

    // Entry methods
    public List<Entry> GetEntriesByTeam(string teamId) =>
        _entries.Find(x => x.TeamId == teamId && !x.Deleted).ToList();

    public Entry? GetEntryById(string id) =>
        _entries.FindById(id);

    public Entry CreateEntry(Entry entry)
    {
        entry.Deleted = false;
        _entries.Insert(entry);
        return entry;
    }

    public bool UpdateEntry(Entry entry)
    {
        entry.UpdatedAt = DateTime.UtcNow;
        return _entries.Update(entry);
    }

    public bool DeleteEntry(string id)
    {
        var entry = GetEntryById(id);
        if (entry == null) return false;
        
        entry.Deleted = true;
        entry.UpdatedAt = DateTime.UtcNow;
        return _entries.Update(entry);
    }

    public List<string> GetCategories()
    {
        return _entries
            .FindAll()
            .Select(e => e.Category)
            .Where(c => !string.IsNullOrEmpty(c))
            .Distinct()
            .OrderBy(c => c)
            .ToList();
    }

    public List<Entry> GetAllEntries() =>
        _entries.FindAll().ToList();
    
    public async Task<List<Entry>> GetEntriesAsync(string search)
    {
        var entries = _entries.Find(e => !e.Deleted).ToList();

        if (!string.IsNullOrWhiteSpace(search))
        {
            entries = entries.Where(e =>
                (e.Title ?? string.Empty).Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (e.Username ?? string.Empty).Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (e.Url ?? string.Empty).Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (e.Notes ?? string.Empty).Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (e.Category ?? string.Empty).Contains(search, StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }

        return await Task.FromResult(entries);
    }
    
    public bool UpdateUser(User user)
    {
        return _users.Update(user);
    }

    public bool DeleteUser(string userId)
    {
        return _users.Delete(userId);
    }
}
