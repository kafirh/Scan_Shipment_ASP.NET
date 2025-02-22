using Microsoft.EntityFrameworkCore;
using ShipmentScan.Data;
using Rotativa.AspNetCore;
using System.IO;
using ShipmentScan.Controllers.Services;
using ShipmentScan.Hubs;
using Microsoft.Extensions.FileSystemGlobbing.Internal.Patterns;

var builder = WebApplication.CreateBuilder(args);

// Konfigurasi session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(480);
    options.Cookie.HttpOnly = true; // Restrict cookie access to server
    options.Cookie.IsEssential = true; // Mark the cookie as essential
});

// Registrasikan IHttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Register services
builder.Services.AddTransient<Service>();

// Ambil connection string dari konfigurasi
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
}

// Tambahkan DbContext untuk aplikasi
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Tambahkan layanan untuk MVC dan SignalR
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

builder.Services.AddDistributedMemoryCache();

// Konfigurasi Rotativa
var rotativaPath = Path.Combine(Directory.GetCurrentDirectory());
Console.WriteLine($"Rotativa Path Configured: {rotativaPath}");
RotativaConfiguration.Setup(rotativaPath);

var app = builder.Build();

// Dynamically get the base path (can be set from environment or app settings)
string basePath = builder.Configuration["BasePath"] ?? "/"; // Default to '/' if not set in config

app.UsePathBase(basePath); // Set the dynamic base path

// Configure the middleware pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles(); // Serve static files
app.UseRouting(); // Routing must come after UsePathBase
app.UseWebSockets(); // WebSockets for SignalR

app.UseSession();
app.UseAuthorization();

// Middleware to restrict access based on user roles
app.Use(async (context, next) =>
{
    string? userRole = context.Session.GetString("UserRole");
    Console.WriteLine($"UserRole: {userRole}");
    Console.WriteLine($"Request Path: {context.Request.Path}");

    if (context.Request.Path.StartsWithSegments("/Home/AccessDenied"))
    {
        await next();
        return;
    }

    var restrictedRoutes = new Dictionary<string, List<string>>
    {
        { "/Scan", new List<string> { "Admin", "InspectorShipmentFG" } },
        { "/Form", new List<string> { "Admin", "PPC" } },
        { "/Home", new List<string> { "Admin", "PPC", "User" } },
        { "/Details", new List<string> { "Admin", "PPC", "User" } }
    };

    foreach (var route in restrictedRoutes)
    {
        if (context.Request.Path.StartsWithSegments(route.Key) &&
            (userRole == null || !route.Value.Contains(userRole)))
        {
            context.Response.Redirect($"{basePath}/Home/AccessDenied"); // Redirect with base path
            return;
        }
    }

    await next();
});

// Configure SignalR endpoints
app.UseEndpoints(endpoints =>
{
    endpoints.MapHub<ShipmentHub>("/shipmentHub"); // Map SignalR hub at the dynamic path
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Login}/{action=Index}/{id?}");
});

app.Run();
