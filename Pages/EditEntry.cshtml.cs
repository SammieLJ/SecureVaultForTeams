using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SecureVaultForTeams.Models;
using SecureVaultForTeams.Services;
using System.Security.Claims;

namespace SecureVaultForTeams.Pages
{
    public class EditEntryModel(DatabaseService dbService, ILogger<EditEntryModel> logger) : PageModel
    {
        private readonly ILogger<EditEntryModel> _logger = logger;

        [BindProperty]
        public Entry? Entry { get; set; } = new();
        public List<Team> Teams { get; set; } = new();

        public List<string> Categories { get; set; } = new();

        public IActionResult OnGet(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            Entry = dbService.GetEntryById(id);
            // Check if entry exists and user has access to it
            if (Entry == null)
            {
                return NotFound();
            }

            // Check if user has access to the entry
            var currentTeamId = User.FindFirstValue("TeamId");
            var isAdmin = User.IsInRole("Admin");
            
            // Only allow access if the user is an admin or if the entry belongs to their team
            if (!isAdmin && Entry.TeamId != currentTeamId)
            {
                return Forbid();
            }
            
            //Categories = dbService.GetCategories(); // Loads Categories that were used in DB, not good! Bad AI!
            
            // Load Categories from JSON file, it might change and categories are always fresh!
            var categoriesPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "categories.json");
            var categories = JsonHelper.SafeReadJsonFile(categoriesPath, new Dictionary<string, string[]>());
            Categories = categories.TryGetValue("PasswordEntryCategories", out var category)
                ? category.ToList()
                : new List<string>();
            
            // Load all Teams from DB
            Teams = dbService.GetTeams();

            return Page();
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                Categories = dbService.GetCategories();
                Teams = dbService.GetTeams();
                return Page();
            }

            // Check if entry exists and user has access to it
            var existingEntry = dbService.GetEntryById(Entry.Id);
            //var existingEntry = Entry;
            if (existingEntry == null)
            {
                return NotFound();
            }

            var currentTeamId = User.FindFirstValue("TeamId");
            var isAdmin = User.IsInRole("Admin");

            if (!isAdmin && existingEntry.TeamId != currentTeamId)
            {
                return Forbid();
            }

            // Only update fields that are editable in the form
            existingEntry.Title = Entry.Title;
            existingEntry.Username = Entry.Username;
            existingEntry.Password = Entry.Password;
            existingEntry.Url = Entry.Url;
            existingEntry.Notes = Entry.Notes;
            existingEntry.Category = Entry.Category;
            existingEntry.TeamId = Entry.TeamId;
            existingEntry.LastModified = DateTime.UtcNow;

            dbService.UpdateEntry(existingEntry);

            return RedirectToPage("/Index");
        }

        public IActionResult OnPostDelete()
        {
            //var entry = dbService.GetEntryById(Entry.Id);
            if (Entry == null)
            {
                return NotFound();
            }

            var isAdmin = User.IsInRole("Admin");
            if (!isAdmin)
            {
                return Forbid();
            }

            Entry.Deleted = true;
            dbService.UpdateEntry(Entry);
            return RedirectToPage("/Index");
        }
    }
}
