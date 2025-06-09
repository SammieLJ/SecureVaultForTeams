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

        public Entry Entry { get; set; } = new(); // For displaying data (GET)

        [BindProperty]
        public Entry Input { get; set; } = new(); // For binding form data (POST)
        public List<Team> Teams { get; set; } = new();

        public List<string> Categories { get; set; } = new();

        public IActionResult OnGet(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            Entry = dbService.GetEntryById(id);
            if (Entry == null)
            {
                return NotFound();
            }
            Entry.Sanitize();
            // Fill Input with Entry values for form binding
            Input = new Entry {
                Id = Entry.Id,
                Title = Entry.Title,
                Username = Entry.Username,
                Password = Entry.Password,
                Url = Entry.Url,
                Notes = Entry.Notes,
                Category = Entry.Category,
                TeamId = Entry.TeamId,
                Created = Entry.Created
            };

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
            var isAdmin = User.IsInRole("Admin");
            if (!isAdmin)
            {
                Input.TeamId = User.FindFirstValue("TeamId") ?? string.Empty;
            }

            if (!ModelState.IsValid)
            {
                // Always reload categories and teams for redisplay
                var categoriesPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "categories.json");
                var categories = JsonHelper.SafeReadJsonFile(categoriesPath, new Dictionary<string, string[]>());
                Categories = categories.TryGetValue("PasswordEntryCategories", out var category)
                    ? category.ToList()
                    : new List<string>();
                Teams = dbService.GetTeams();
                return Page();
            }

            var existingEntry = dbService.GetEntryById(Input.Id);
            if (existingEntry == null)
            {
                return NotFound();
            }

            var currentTeamId = User.FindFirstValue("TeamId");
            isAdmin = User.IsInRole("Admin");

            if (!isAdmin && existingEntry.TeamId != currentTeamId)
            {
                return Forbid();
            }

            // Only update fields that are editable in the form
            existingEntry.Title = Input.Title;
            existingEntry.Username = Input.Username;
            existingEntry.Password = Input.Password;
            existingEntry.Url = Input.Url;
            existingEntry.Notes = Input.Notes;
            existingEntry.Category = Input.Category;
            existingEntry.TeamId = Input.TeamId;
            existingEntry.LastModified = DateTime.UtcNow;

            dbService.UpdateEntry(existingEntry);

            return RedirectToPage("/Index");
        }

        public IActionResult OnPostDelete()
        {
            if (Input == null)
            {
                return NotFound();
            }

            var isAdmin = User.IsInRole("Admin");
            if (!isAdmin)
            {
                return Forbid();
            }
            
            var existingEntry = dbService.GetEntryById(Input.Id);
            if (existingEntry == null)
            {
                return NotFound();
            }
            existingEntry.Deleted = true;
            existingEntry.LastModified = DateTime.UtcNow;
            dbService.UpdateEntry(existingEntry);
            
            //Input.Deleted = true;
            //dbService.UpdateEntry(Input);
            return RedirectToPage("/Index");
        }
    }
}
