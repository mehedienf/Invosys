using InventoryTracker.Data;
using InventoryTracker.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Connection String পড়ুন
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                       ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// 2. ApplicationDbContext রেজিস্ট্রেশন করুন (MySQL)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
});

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Database Migration এবং Seed Data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    // Database তৈরি করুন (যদি না থাকে)
    context.Database.EnsureCreated();
    
    // Seed Data (যদি কোনো পণ্য না থাকে)
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
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();