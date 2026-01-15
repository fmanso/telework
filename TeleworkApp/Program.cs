using Microsoft.EntityFrameworkCore;
using TeleworkApp.Components;
using TeleworkApp.Data;
using TeleworkApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel for desktop mode (specific port for Electron)
builder.WebHost.UseUrls("http://localhost:5123");

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure SQLite - database stored in same folder as the app
var dbPath = Path.Combine(AppContext.BaseDirectory, "telework.db");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// Register services
builder.Services.AddScoped<HolidayService>();
builder.Services.AddScoped<TeleworkService>();

var app = builder.Build();

// Handle graceful shutdown
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(() =>
{
    Console.WriteLine("Application is shutting down...");
});

// Create database automatically
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

// Disable HTTPS redirect for desktop app (Electron uses HTTP locally)
// app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
