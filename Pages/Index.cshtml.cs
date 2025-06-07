using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SecureVaultForTeams.Models;
using SecureVaultForTeams.Services;
using System.Security.Claims;

namespace SecureVaultForTeams.Pages;

public class IndexModel : PageModel
{
    private readonly DatabaseService _dbService;
    public List<Entry> Entries { get; set; } = new();
    public List<string> Categories { get; set; } = new();

    public string CurrentTeamId { get; set; } = string.Empty;
    public string SearchTerm { get; set; } = string.Empty;
    public int CurrentPage { get; set; } = 1;

    private const int ItemsPerPage = 10;
    public int PageSize { get; set; } = 10; // Ensure consistent page size
    public int TotalPages { get; set; } = 1;
    public bool ShowPagination { get; set; } = false;
    
    // Za CreateEntry page:
    [BindProperty]
    public Entry? Entry { get; set; } = new();
    
    public List<Team> Teams { get; set; } = new();
    
    public IndexModel(DatabaseService dbService)
    {
        _dbService = dbService;
    }
    //IActionResult
    public IActionResult OnGet(string? teamId = null, string? search = null, string? category = null, bool showDeleted = false, int currentPage = 1, int pageSize = ItemsPerPage)
    {
        var isAdmin = User.IsInRole("Admin");
        CurrentTeamId = teamId ?? User.FindFirst("TeamId")?.Value ?? string.Empty;
        SearchTerm = search ?? string.Empty;
        CurrentPage = currentPage;
        PageSize = pageSize;
        List<Entry> entries;
        
        if (isAdmin)
        {
            entries = _dbService.GetAllEntries()
                .Where(e => (showDeleted ? e.Deleted : !e.Deleted) && (string.IsNullOrEmpty(e.TeamId) || !string.IsNullOrEmpty(e.TeamId) || e.TeamId == null))
                .ToList();
        }
        else if (!string.IsNullOrEmpty(CurrentTeamId))
        {
            entries = _dbService.GetEntriesByTeam(CurrentTeamId);
        }
        else
        {
            entries = new List<Entry>();
        }

        // Sanitize all entries before further processing
        foreach (var entry in entries)
        {
            entry.Sanitize();
        }

        if (!string.IsNullOrEmpty(SearchTerm))
        {
            entries = entries.Where(e =>
                e.Title.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                e.Username.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                e.Url.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                e.Notes.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                e.Category.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                e.TeamName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }

        if (!string.IsNullOrEmpty(category))
        {
            entries = entries.Where(e => e.Category == category).ToList();
        }

        // Use filteredEntries for categories and pagination
        var filteredEntries = entries;

        // Get unique categories for the filter dropdown (from filtered, not paginated list)
        Categories = filteredEntries
            .Select(e => e.Category)
            .Distinct()
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .OrderBy(c => c)
            .ToList();

        // Calculate total pages
        TotalPages = (int)Math.Ceiling(filteredEntries.Count / (double)PageSize);
        if (TotalPages == 0) TotalPages = 1;

        // Ensure current page is valid
        if (CurrentPage < 1) CurrentPage = 1;
        if (CurrentPage > TotalPages) CurrentPage = TotalPages;

        // Show pagination if more than one page
        ShowPagination = TotalPages > 1;

        // Get entries for current page
        Entries = filteredEntries
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        // Fill TeamNames for each entry
        _dbService.PopulateTeamNames(Entries);
        
        if (entries == null)
        {
            entries = new List<Entry>();
        }

        return Page();
    }
    
    /*public async Task<IActionResult> OnGetPagedAsync(int page = 1, int pageSize = ItemsPerPage, string search = null)
    {
        // Fetch filtered entries
        var entries = await _dbService.GetEntriesAsync(search);

        // Apply pagination
        Entries = entries.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        CurrentPage = page;
        TotalPages = (int)Math.Ceiling(entries.Count / (double)pageSize);
        return Page();
    }*/

    public IActionResult OnPostTogglePassword(string entryId)
    {
        var entry = _dbService.GetEntryById(entryId);
        if (entry != null)
        {
            return new JsonResult(new { password = entry.Password });
        }
        return NotFound();
    }
    
    public IActionResult OnGetCreateEntry()
    {
        var categoriesPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "categories.json");
        var categories = JsonHelper.SafeReadJsonFile(categoriesPath, new Dictionary<string, string[]>());
        var categoryList = categories.TryGetValue("PasswordEntryCategories", out var category)
            ? category.ToList()
            : new List<string>();
        var teams = _dbService.GetTeams().Select(t => new { id = t.Id, name = t.Name }).ToList();
        return new JsonResult(new { categories = categoryList, teams });
    }

    public IActionResult OnPostCreateEntry()
    {
        if (!ModelState.IsValid)
        {
            Teams = _dbService.GetTeams();
            //entries = _dbService.GetEntriesByTeam(CurrentTeamId); // for non-admin users
            CurrentTeamId = User.FindFirst("TeamId")?.Value ?? string.Empty;
            OnGet(CurrentTeamId, SearchTerm, null, false, CurrentPage, PageSize);
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
            Teams = _dbService.GetTeams();
            ModelState.AddModelError(string.Empty, "TeamId is required.");
            return Page();
        }

        _dbService.CreateEntry(Entry);
        return RedirectToPage("/Index");
    }
}
