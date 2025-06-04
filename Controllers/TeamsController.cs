using Microsoft.AspNetCore.Mvc;
using SecureVaultForTeams.Services;
using SecureVaultForTeams.Models;

namespace SecureVaultForTeams.Controllers
{
    [ApiController]
    [Route("api/teams")]
    public class TeamsController : ControllerBase
    {
        private readonly DatabaseService _dbService;
        public TeamsController(DatabaseService dbService)
        {
            _dbService = dbService;
        }

        [HttpGet("{id}")]
        public IActionResult GetTeamById(string id)
        {
            var team = _dbService.GetTeamById(id);
            if (team == null)
                return NotFound();
            return Ok(new {
                id = team.Id,
                name = team.Name,
                description = team.Description
            });
        }
    }
}

