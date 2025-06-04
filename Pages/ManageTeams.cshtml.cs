using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SecureVaultForTeams.Models;
using SecureVaultForTeams.Services;

namespace SecureVaultForTeams.Pages
{
    [Authorize(Roles = "Admin")]
    public class ManageTeamsModel : PageModel
    {
        private readonly DatabaseService _dbService;
        private readonly ILogger<ManageTeamsModel> _logger;

        public ManageTeamsModel(DatabaseService dbService, ILogger<ManageTeamsModel> logger)
        {
            _dbService = dbService;
            _logger = logger;
        }

        public List<TeamViewModel> Teams { get; set; } = new();

        public void OnGet()
        {
            var teams = _dbService.GetTeams();
            Teams = teams.Select(t => new TeamViewModel
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                Created = t.Created,
                MemberCount = _dbService.GetTeamMemberCount(t.Id)
            }).ToList();
        }

        public IActionResult OnPostCreateTeam(Team team)
        {
            if (string.IsNullOrWhiteSpace(team.Name))
            {
                ModelState.AddModelError("Name", "Team name is required");
                return Page();
            }

            team.Created = DateTime.UtcNow;
            _dbService.CreateTeam(team);
            
            return RedirectToPage();
        }

        public IActionResult OnPostUpdateTeam(Team team)
        {
            if (string.IsNullOrWhiteSpace(team.Name))
            {
                ModelState.AddModelError("Name", "Team name is required");
                return Page();
            }

            var existingTeam = _dbService.GetTeamById(team.Id);
            if (existingTeam == null)
            {
                return NotFound();
            }

            existingTeam.Name = team.Name;
            existingTeam.Description = team.Description;
            
            _dbService.UpdateTeam(existingTeam);
            
            return RedirectToPage();
        }

        public IActionResult OnPostDeleteTeam(string id)
        {
            var team = _dbService.GetTeamById(id);
            if (team == null)
            {
                return NotFound();
            }

            _dbService.DeleteTeam(id);
            
            return RedirectToPage();
        }
    }

    public class TeamViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Created { get; set; }
        public int MemberCount { get; set; }
    }
}
