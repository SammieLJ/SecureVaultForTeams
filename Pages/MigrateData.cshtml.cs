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
    public string? HashResult { get; set; }
    public string? VerifyResult { get; set; }

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
        if (action == "hash")
        {
            var plainPassword = Request.Form["plainPassword"].ToString();
            if (!string.IsNullOrEmpty(plainPassword))
            {
                HashResult = BCrypt.Net.BCrypt.HashPassword(plainPassword);
            }
            else
            {
                HashResult = "Please enter a password to hash.";
            }
            return Page();
        }
        if (action == "verify")
        {
            var hashToCheck = Request.Form["hashToCheck"].ToString();
            var passwordToCheck = Request.Form["passwordToCheck"].ToString();
            if (!string.IsNullOrEmpty(hashToCheck) && !string.IsNullOrEmpty(passwordToCheck))
            {
                bool isValid = BCrypt.Net.BCrypt.Verify(passwordToCheck, hashToCheck);
                VerifyResult = isValid ? "Password matches the hash!" : "Password does NOT match the hash.";
            }
            else
            {
                VerifyResult = "Please enter both a hash and a password to check.";
            }
            return Page();
        }
        Message = "Unknown action.";
        return Page();
    }
}
