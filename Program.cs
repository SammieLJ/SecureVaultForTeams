var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Configure Razor Pages options - MOVED THIS BEFORE app.Build()
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AllowAnonymousToPage("/Privacy");
});

builder.Services.AddAuthentication(options => 
{
    options.DefaultScheme = "Cookies";
    options.DefaultChallengeScheme = "Cookies";
}).AddCookie("Cookies", options => 
{
    options.LoginPath = "/Login";
    options.LogoutPath = "/Logout";
    options.AccessDeniedPath = "/Error";
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
});
builder.Services.AddAuthorization();

// Add our services
builder.Services.AddSingleton<SecureVaultForTeams.Services.DatabaseService>();
builder.Services.AddScoped<SecureVaultForTeams.Services.AuthService>();

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllers();

// Configure pages that require authorization (all except Privacy)
var razorPages = app.MapRazorPages();
razorPages.RequireAuthorization();

// Create admin user if it doesn't exist
using (var scope = app.Services.CreateScope())
{
    var dbService = scope.ServiceProvider.GetRequiredService<SecureVaultForTeams.Services.DatabaseService>();
    var authService = scope.ServiceProvider.GetRequiredService<SecureVaultForTeams.Services.AuthService>();
    
    if (dbService.GetUserByUsername("admin") == null)
    {
        var adminId = authService.CreateUser("admin", "Admin123!", "Admin");
        
        // Ensure we have at least one team for the admin
        var teams = dbService.GetTeams();
        if (!teams.Any())
        {
            var defaultTeam = new SecureVaultForTeams.Models.Team
            {
                Name = "Default Team",
                Description = "Default team created at startup"
            };
            dbService.CreateTeam(defaultTeam);
        }
    }
}

app.Run();
