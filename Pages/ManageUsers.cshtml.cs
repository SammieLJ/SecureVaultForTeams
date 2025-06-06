using Microsoft.AspNetCore.Mvc.RazorPages;
using SecureVaultForTeams.Models;
using SecureVaultForTeams.Services;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using BCrypt.Net;
using System.Linq;

namespace SecureVaultForTeams.Pages
{
    public class ManageUsersModel : PageModel
    {
        private readonly DatabaseService _dbService;
        public List<User> Users { get; set; } = new();
        public List<Team> Teams { get; set; } = new();

        [BindProperty]
        public CreateUserInput Input { get; set; }
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        public ManageUsersModel(DatabaseService dbService)
        {
            _dbService = dbService;
        }

        public void OnGet()
        {
            Users = _dbService.GetAllUsers();
            Teams = _dbService.GetAllTeams();
            _dbService.PopulateTeamNames(Users);
        }

        private bool IsInputModelValid()
        {
            return ModelState
                .Where(kvp => kvp.Key.StartsWith("Input."))
                .All(kvp => kvp.Value.Errors.Count == 0);
        }

        public IActionResult OnPost()
        {
            Users = _dbService.GetAllUsers();
            Teams = _dbService.GetAllTeams();
            if (!IsInputModelValid())
            {
                ErrorMessage = "Please fill in all required fields.";
                return Page();
            }
            if (Input.TeamIds == null || !Input.TeamIds.Any())
            {
                ErrorMessage = "Please select at least one team.";
                return Page();
            }
            if (_dbService.GetUserByUsername(Input.Username) != null)
            {
                ErrorMessage = "Username already exists.";
                return Page();
            }
            var user = new User
            {
                Username = Input.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Input.Password),
                Role = Input.Role,
                TeamIds = Input.TeamIds ?? new List<string>()
            };
            _dbService.CreateUser(user);
            SuccessMessage = "User created successfully.";
            Users = _dbService.GetAllUsers();
            Teams = _dbService.GetAllTeams();
            //return RedirectToPage();
            return Page();
        }

        public IActionResult OnPostEdit()
        {
            Users = _dbService.GetAllUsers();
            if (!ModelState.IsValid)
            {
                ErrorMessage = "Please fill in all required fields.";
                return Page();
            }
            var user = Users.FirstOrDefault(u => u.Id == EditInput.Id);
            if (user == null)
            {
                ErrorMessage = "User not found.";
                return Page();
            }
            // Check for username change and uniqueness
            if (!string.Equals(user.Username, EditInput.Username, StringComparison.OrdinalIgnoreCase) &&
                _dbService.GetUserByUsername(EditInput.Username) != null)
            {
                ErrorMessage = "Username already exists.";
                return Page();
            }
            user.Username = EditInput.Username;
            if (!string.IsNullOrWhiteSpace(EditInput.Password))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(EditInput.Password);
            }
            user.Role = EditInput.Role;
            if (_dbService.UpdateUser(user))
                SuccessMessage = "User updated successfully.";
            else
                ErrorMessage = "Failed to update user.";
            Users = _dbService.GetAllUsers();
            return RedirectToPage();
        }

        [BindProperty]
        public EditUserInput EditInput { get; set; }

        public class EditUserInput
        {
            [Required]
            public string Id { get; set; }
            [Required]
            public string Username { get; set; }
            public string Password { get; set; }
            [Required]
            public string Role { get; set; }
        }

        public IActionResult OnPostDelete()
        {
            Users = _dbService.GetAllUsers();
            var user = Users.FirstOrDefault(u => u.Id == DeleteInput.Id);
            if (user == null)
            {
                ErrorMessage = "User not found.";
                return Page();
            }
            if (_dbService.DeleteUser(user.Id))
                SuccessMessage = "User deleted successfully.";
            else
                ErrorMessage = "Failed to delete user.";
            Users = _dbService.GetAllUsers();
            return RedirectToPage();
        }

        [BindProperty]
        public DeleteUserInput DeleteInput { get; set; }

        public class DeleteUserInput
        {
            [Required]
            public string Id { get; set; }
        }

        public class CreateUserInput
        {
            [Required]
            public string Username { get; set; }
            [Required]
            public string Password { get; set; }
            [Required]
            public string Role { get; set; }
            public List<string> TeamIds { get; set; } = new();
        }
    }
}
