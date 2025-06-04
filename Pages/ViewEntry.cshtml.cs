using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SecureVaultForTeams.Models;
using SecureVaultForTeams.Services;
using System.Security.Claims;

namespace SecureVaultForTeams.Pages
{
    public class ViewEntryModel : PageModel
    {
        private readonly DatabaseService _dbService;

        public ViewEntryModel(DatabaseService dbService)
        {
            _dbService = dbService;
        }

        public Entry? Entry { get; set; }

        public IActionResult OnGet(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            Entry = _dbService.GetEntryById(id);
            
            if (Entry != null && !string.IsNullOrEmpty(Entry.TeamId))
            {
                // Display Team info name for View
                Entry.TeamName = _dbService.GetTeamById(Entry.TeamId)?.Name;
            }
            else
            {
                if (Entry != null) Entry.TeamName = "No team defined!";
            }
            
            // Check if entry exists and user has access to it
            if (Entry == null)
            {
                return NotFound();
            }

            var currentTeamId = User.FindFirstValue("TeamId");
            var isAdmin = User.IsInRole("Admin");

            // Only allow access if the user is an admin or if the entry belongs to their team
            if (!isAdmin && Entry.TeamId != currentTeamId)
            {
                return Forbid();
            }
            
            return Page();
        }
    }
}
