using LiteDB;
using SecureVaultForTeams.Models;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

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

    // Populates TeamName for each entry that has a TeamId
    public void PopulateTeamNames(List<Entry> entries)
    {
        var teams = GetTeams().ToDictionary(t => t.Id, t => t.Name);
        foreach (var entry in entries)
        {
            if (!string.IsNullOrEmpty(entry.TeamId) && teams.TryGetValue(entry.TeamId, out var teamName))
            {
                entry.TeamName = teamName;
            }
            else
            {
                entry.TeamName = string.Empty;
            }
        }
    }

    public List<Entry> GetAllEntries()
    {
        var entries = _entries.FindAll().ToList();
        PopulateTeamNames(entries);
        return entries;
    }
    
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
                (e.Category ?? string.Empty).Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (e.TeamName ?? string.Empty).Contains(search, StringComparison.OrdinalIgnoreCase)
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

    // Populates TeamNames for each user that has TeamIds
    public void PopulateTeamNames(List<User> users)
    {
        var teams = GetTeams().ToDictionary(t => t.Id, t => t.Name);
        foreach (var user in users)
        {
            if (user.TeamIds != null && user.TeamIds.Count > 0)
            {
                var names = user.TeamIds
                    .Where(id => teams.ContainsKey(id))
                    .Select(id => teams[id])
                    .ToList();
                user.TeamNames = names.Count > 0 ? string.Join(", ", names) : string.Empty;
            }
            else
            {
                user.TeamNames = string.Empty;
            }
        }
    }

    // Populates TeamNames for a single user
    public void PopulateTeamNames(User user)
    {
        var teams = GetTeams().ToDictionary(t => t.Id, t => t.Name);
        if (user.TeamIds != null && user.TeamIds.Count > 0)
        {
            var names = user.TeamIds
                .Where(id => teams.ContainsKey(id))
                .Select(id => teams[id])
                .ToList();
            user.TeamNames = names.Count > 0 ? string.Join(", ", names) : string.Empty;
        }
        else
        {
            user.TeamNames = string.Empty;
        }
    }

    // Bulk replace methods for import
    public void ReplaceAllEntries(List<Entry> entries)
    {
        _entries.DeleteAll();
        if (entries != null && entries.Count > 0)
            _entries.InsertBulk(entries);
    }

    public void ReplaceAllUsers(List<User> users)
    {
        _users.DeleteAll();
        if (users != null && users.Count > 0)
            _users.InsertBulk(users);
    }

    public void ReplaceAllTeams(List<Team> teams)
    {
        _teams.DeleteAll();
        if (teams != null && teams.Count > 0)
            _teams.InsertBulk(teams);
    }

    public class MigrationData
    {
        public List<User> Users { get; set; } = new();
        public List<Team> Teams { get; set; } = new();
        public List<Entry> Entries { get; set; } = new();
    }

    public void ExportToJson(string path)
    {
        var data = new MigrationData
        {
            Users = GetAllUsers(),
            Teams = GetAllTeams(),
            Entries = GetAllEntries()
        };
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(data, options);
        File.WriteAllText(path, json);
    }

    public void ImportFromJson(string path)
    {
        if (!File.Exists(path)) return;
        var json = File.ReadAllText(path);
        var data = JsonSerializer.Deserialize<MigrationData>(json);
        if (data != null)
        {
            ReplaceAllUsers(data.Users);
            ReplaceAllTeams(data.Teams);
            ReplaceAllEntries(data.Entries);
        }
    }
}
