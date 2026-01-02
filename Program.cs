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

    // Seed Products (যদি কোনো পণ্য না থাকে)
    if (!context.Products.Any())
    {
        var products = new List<Product>
        {
            new Product { Name = "চাল (মিনিকেট)", Description = "উন্নত মানের মিনিকেট চাল", Price = 65.00M, Quantity = 100, Category = "খাদ্যশস্য", CreatedAt = DateTime.Now },
            new Product { Name = "ডাল (মুগ)", Description = "দেশী মুগ ডাল", Price = 120.00M, Quantity = 50, Category = "খাদ্যশস্য", CreatedAt = DateTime.Now },
            new Product { Name = "সয়াবিন তেল (১ লিটার)", Description = "তাজা সয়াবিন তেল", Price = 180.00M, Quantity = 30, Category = "তেল", CreatedAt = DateTime.Now },
            new Product { Name = "চিনি", Description = "সাদা চিনি", Price = 110.00M, Quantity = 40, Category = "মিষ্টি দ্রব্য", CreatedAt = DateTime.Now },
            new Product { Name = "লবণ (১ কেজি)", Description = "আয়োডিন যুক্ত লবণ", Price = 30.00M, Quantity = 80, Category = "মসলা", CreatedAt = DateTime.Now },
            new Product { Name = "হলুদ গুঁড়া", Description = "বিশুদ্ধ হলুদ গুঁড়া", Price = 200.00M, Quantity = 25, Category = "মসলা", CreatedAt = DateTime.Now },
            new Product { Name = "মরিচ গুঁড়া", Description = "ঝাল মরিচ গুঁড়া", Price = 250.00M, Quantity = 20, Category = "মসলা", CreatedAt = DateTime.Now },
            new Product { Name = "আটা (২ কেজি)", Description = "গমের আটা", Price = 90.00M, Quantity = 60, Category = "খাদ্যশস্য", CreatedAt = DateTime.Now },
            new Product { Name = "সাবান (লাক্স)", Description = "সুগন্ধি সাবান", Price = 45.00M, Quantity = 5, Category = "প্রসাধনী", CreatedAt = DateTime.Now },
            new Product { Name = "শ্যাম্পু", Description = "চুলের শ্যাম্পু", Price = 150.00M, Quantity = 15, Category = "প্রসাধনী", CreatedAt = DateTime.Now }
        };

        context.Products.AddRange(products);
        context.SaveChanges();
    }

    // Seed Shop Info (যদি কোনো তথ্য না থাকে)
    if (!context.ShopInfos.Any())
    {
        var shopInfo = new ShopInfo
        {
            ShopName = "আমাদের দোকান",
            Phone = "+880 1XXX-XXXXXX",
            Email = "info@shop.com",
            Address = "ঢাকা, বাংলাদেশ",
            OpeningHours = "সোম - রবি, ৯ AM - ৯ PM",
            Description = "আমরা উচ্চমানের পণ্য সরবরাহ করি এবং সর্বোত্তম গ্রাহক সেবা প্রদান করি।",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
        context.ShopInfos.Add(shopInfo);
        context.SaveChanges();
    }

    // Seed Sales Transactions (যদি কোনো বিক্রয় না থাকে)
    if (!context.SalesTransactions.Any())
    {
        var salesTransactions = new List<SalesTransaction>
        {
            new SalesTransaction
            {
                SaleDate = DateTime.Now.AddDays(-5),
                CustomerName = "রফিকুল ইসলাম",
                TotalAmount = 325.00M,
                Notes = "নিয়মিত গ্রাহক"
            },
            new SalesTransaction
            {
                SaleDate = DateTime.Now.AddDays(-3),
                CustomerName = "নাজমা বেগম",
                TotalAmount = 420.00M,
                Notes = "বাল্ক অর্ডার"
            },
            new SalesTransaction
            {
                SaleDate = DateTime.Now.AddDays(-1),
                CustomerName = "করিম আহমেদ",
                TotalAmount = 540.00M,
                Notes = "নগদ পেমেন্ট"
            },
            new SalesTransaction
            {
                SaleDate = DateTime.Now,
                CustomerName = "ফাতিমা খান",
                TotalAmount = 650.00M,
                Notes = "দ্রুত ডেলিভারি"
            }
        };

        context.SalesTransactions.AddRange(salesTransactions);
        context.SaveChanges();

        // এখন Sales Items যোগ করুন (প্রতিটি transaction এর জন্য)
        var allProducts = context.Products.ToList();
        var allTransactions = context.SalesTransactions.ToList();

        if (allTransactions.Count > 0 && allProducts.Count > 0)
        {
            var salesItems = new List<SalesItem>();

            // Transaction 1: চাল এবং ডাল
            salesItems.Add(new SalesItem
            {
                TransactionId = allTransactions[0].Id,
                ProductId = allProducts[0].Id, // চাল
                QuantitySold = 5,
                UnitPrice = 65.00M
            });
            salesItems.Add(new SalesItem
            {
                TransactionId = allTransactions[0].Id,
                ProductId = allProducts[1].Id, // ডাল
                QuantitySold = 2,
                UnitPrice = 120.00M
            });

            // Transaction 2: তেল এবং চিনি
            salesItems.Add(new SalesItem
            {
                TransactionId = allTransactions[1].Id,
                ProductId = allProducts[2].Id, // তেল
                QuantitySold = 2,
                UnitPrice = 180.00M
            });
            salesItems.Add(new SalesItem
            {
                TransactionId = allTransactions[1].Id,
                ProductId = allProducts[3].Id, // চিনি
                QuantitySold = 1,
                UnitPrice = 110.00M
            });

            // Transaction 3: আটা এবং লবণ
            salesItems.Add(new SalesItem
            {
                TransactionId = allTransactions[2].Id,
                ProductId = allProducts[7].Id, // আটা
                QuantitySold = 3,
                UnitPrice = 90.00M
            });
            salesItems.Add(new SalesItem
            {
                TransactionId = allTransactions[2].Id,
                ProductId = allProducts[4].Id, // লবণ
                QuantitySold = 5,
                UnitPrice = 30.00M
            });

            // Transaction 4: শ্যাম্পু এবং সাবান
            salesItems.Add(new SalesItem
            {
                TransactionId = allTransactions[3].Id,
                ProductId = allProducts[9].Id, // শ্যাম্পু
                QuantitySold = 2,
                UnitPrice = 150.00M
            });
            salesItems.Add(new SalesItem
            {
                TransactionId = allTransactions[3].Id,
                ProductId = allProducts[8].Id, // সাবান
                QuantitySold = 4,
                UnitPrice = 45.00M
            });

            context.SalesItems.AddRange(salesItems);
            context.SaveChanges();
        }
    }
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