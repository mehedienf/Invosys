using InventoryTracker.Data;
using InventoryTracker.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// PORT select from environment variable
builder.WebHost.ConfigureKestrel(options =>
{
    var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
    options.ListenAnyIP(int.Parse(port));
});

// Connection String read
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// ApplicationDbContext register
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlite(connectionString);
});

// Authentication setup
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Database Migration এবং Seed Data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // Database তৈরি করুন (যদি না থাকে)
    context.Database.EnsureCreated();

    // Seed Users only
    if (!context.Users.Any())
    {
        // Password hash function
        string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        var users = new List<User>
        {
            // System admin - protected from deletion and editing
            new User
            {
                Username = "system",
                PasswordHash = HashPassword("system@123"),
                FullName = "সিস্টেম অ্যাডমিনিস্ট্রেটর",
                Role = "Admin",
                IsActive = true,
                CreatedAt = DateTime.Now
            },
            new User
            {
                Username = "admin",
                PasswordHash = HashPassword("admin123"),
                FullName = "প্রধান অ্যাডমিন",
                Role = "Admin",
                IsActive = true,
                CreatedAt = DateTime.Now
            },
            new User
            {
                Username = "staff",
                PasswordHash = HashPassword("staff123"),
                FullName = "সেলস স্টাফ",
                Role = "Staff",
                IsActive = true,
                CreatedAt = DateTime.Now
            }
        };

        context.Users.AddRange(users);
        context.SaveChanges();
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.MapDefaultControllerRoute();
app.UseRouting();

// Authentication & Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();