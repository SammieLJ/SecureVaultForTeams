using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using SecureVaultForTeams.Models;
using SecureVaultForTeams.Services;

namespace SecureVaultForTeams.Pages;

public class CreateEntryModel(DatabaseService dbService) : PageModel
{
    [BindProperty]
    public Entry Entry { get; set; } = new();

    public List<string> Categories { get; set; } = new();
    public List<Team> Teams { get; set; } = new();

    public void OnGet()
    {
        var categoriesPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "categories.json");
        var categories = JsonHelper.SafeReadJsonFile(categoriesPath, new Dictionary<string, string[]>());
        // Always load PasswordEntryCategories, since only password entries are allowed
        Categories = categories.TryGetValue("PasswordEntryCategories", out var category)
            ? category.ToList()
            : [];
        Teams = dbService.GetTeams();
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            Teams = dbService.GetTeams();
            return Page();
        }

        Entry.CreatedBy = User.Identity?.Name ?? "Unknown";
        var isAdmin = User.IsInRole("Admin");
        if (!isAdmin)
        {
            Entry.TeamId = User.FindFirst("TeamId")?.Value ?? string.Empty;
        }
        Entry.Deleted = false;
        Entry.CreatedAt = DateTime.UtcNow;
        Entry.UpdatedAt = DateTime.UtcNow;
        Entry.Created = DateTime.UtcNow;
        Entry.LastModified = DateTime.UtcNow;

        if (string.IsNullOrEmpty(Entry.TeamId))
        {
            Teams = dbService.GetTeams();
            ModelState.AddModelError(string.Empty, "TeamId is required.");
            return Page();
        }

        dbService.CreateEntry(Entry);
        return RedirectToPage("/Index");
    }
}
