using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SecureVaultForTeams.Services;
using System.IO;
using System.Threading.Tasks;

[Authorize(Roles = "Admin")]
public class MigrateDataModel : PageModel
{
    private readonly DatabaseService _dbService;
    private readonly IWebHostEnvironment _env;
    public string? Message { get; set; }

    public MigrateDataModel(DatabaseService dbService, IWebHostEnvironment env)
    {
        _dbService = dbService;
        _env = env;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(string action)
    {
        var dataPath = Path.Combine(_env.ContentRootPath, "Data", "data.json");
        if (action == "export")
        {
            _dbService.ExportToJson(dataPath);
            Message = $"Exported all data to Data/data.json.";
            return Page();
        }
        if (action == "import")
        {
            var file = Request.Form.Files["importFile"];
            if (file == null || file.Length == 0)
            {
                Message = "Please select a data.json file to import.";
                return Page();
            }
            using (var stream = new FileStream(dataPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            _dbService.ImportFromJson(dataPath);
            Message = $"Imported data from Data/data.json and replaced all LiteDB data.";
            return Page();
        }
        Message = "Unknown action.";
        return Page();
    }
}

