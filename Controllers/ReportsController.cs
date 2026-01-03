using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using InventoryTracker.Data;
using InventoryTracker.Models;
using System.Security.Cryptography;
using System.Text;

namespace InventoryTracker.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Reports
        public async Task<IActionResult> Index()
        {
            // Summary data
            ViewBag.TotalProducts = await _context.Products.CountAsync();
            ViewBag.TotalStock = await _context.Products.SumAsync(p => p.Quantity);
            
            // SQLite doesn't support Sum on decimal, so we fetch data and aggregate on client side
            var stockValues = await _context.Products
                .Select(p => p.Price * p.Quantity)
                .ToListAsync();
            ViewBag.TotalStockValue = stockValues.Sum();
            
            ViewBag.TotalSalesCount = await _context.SalesTransactions.CountAsync();
            
            // Fetch sales data and aggregate on client side
            var revenues = await _context.SalesTransactions
                .Select(s => s.FinalAmount)
                .ToListAsync();
            ViewBag.TotalRevenue = revenues.Sum();
            
            // Low stock products
            ViewBag.LowStockProducts = await _context.Products
                .Where(p => p.Quantity < 10)
                .OrderBy(p => p.Quantity)
                .ToListAsync();
            
            // Top selling products - fetch all data and aggregate on client side
            var products = await _context.Products.ToListAsync();
            var salesItems = await _context.SalesItems.Include(s => s.Product).ToListAsync();
            
            var topProducts = salesItems
                .GroupBy(si => si.ProductId)
                .Select(g => new {
                    ProductId = g.Key,
                    ProductName = g.FirstOrDefault()?.Product?.Name ?? products.FirstOrDefault(p => p.Id == g.Key)?.Name ?? "অজানা পণ্য",
                    TotalSold = g.Sum(x => x.QuantitySold),
                    TotalRevenue = g.Sum(x => x.QuantitySold * x.UnitPrice)
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(10)
                .ToList();
            
            ViewBag.TopProducts = topProducts;
            
            // Recent sales
            ViewBag.RecentSales = await _context.SalesTransactions
                .OrderByDescending(s => s.SaleDate)
                .Take(10)
                .ToListAsync();
            
            // Monthly sales (last 6 months)
            var sixMonthsAgo = DateTime.Now.AddMonths(-6);
            var monthlySalesData = await _context.SalesTransactions
                .Where(s => s.SaleDate >= sixMonthsAgo)
                .ToListAsync();
            
            ViewBag.MonthlySales = monthlySalesData
                .GroupBy(s => new { s.SaleDate.Year, s.SaleDate.Month })
                .Select(g => new {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    TotalSales = g.Count(),
                    TotalRevenue = g.Sum(x => x.FinalAmount)
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToList();
            
            return View();
        }

        // GET: Reports/Sales
        public async Task<IActionResult> Sales(DateTime? startDate, DateTime? endDate)
        {
            var query = _context.SalesTransactions
                .Include(s => s.SalesItems!)
                .ThenInclude(si => si.Product)
                .AsQueryable();

            // যদি কোনো ডেট নির্বাচিত না হয় তাহলে সব ডেটা দেখান
            if (!startDate.HasValue && !endDate.HasValue)
            {
                // সব বিক্রয় ডেটা দেখান
            }
            else
            {
                if (startDate.HasValue)
                {
                    query = query.Where(s => s.SaleDate.Date >= startDate.Value.Date);
                    ViewBag.StartDate = startDate.Value.ToString("yyyy-MM-dd");
                }

                if (endDate.HasValue)
                {
                    query = query.Where(s => s.SaleDate.Date <= endDate.Value.Date);
                    ViewBag.EndDate = endDate.Value.ToString("yyyy-MM-dd");
                }
            }

            var sales = await query.OrderByDescending(s => s.SaleDate).ToListAsync();
            
            ViewBag.TotalSales = sales.Count;
            ViewBag.TotalRevenue = sales.Sum(s => s.FinalAmount);
            
            // বেশি বিক্রয় হওয়া পণ্য - যে পণ্যগুলো সবচেয়ে বেশি বিক্রিত হয়েছে
            var products = await _context.Products.ToListAsync();
            var salesItems = sales.SelectMany(s => s.SalesItems ?? new List<SalesItem>()).ToList();
            
            var topSellingProducts = salesItems
                .GroupBy(si => si.ProductId)
                .Select(g => new {
                    ProductId = g.Key,
                    ProductName = products.FirstOrDefault(p => p.Id == g.Key)?.Name ?? "অজানা পণ্য",
                    TotalQuantity = g.Sum(x => x.QuantitySold),
                    TotalRevenue = g.Sum(x => x.QuantitySold * x.UnitPrice)
                })
                .OrderByDescending(x => x.TotalQuantity)
                .ToList();
            
            ViewBag.TopSellingProducts = topSellingProducts;
            
            return View(sales);
        }

        // GET: Reports/Inventory
        public async Task<IActionResult> Inventory()
        {
            var products = await _context.Products.OrderBy(p => p.Name).ToListAsync();
            
            ViewBag.TotalProducts = products.Count;
            ViewBag.TotalStock = products.Sum(p => p.Quantity);
            ViewBag.TotalValue = products.Sum(p => p.Price * p.Quantity);
            ViewBag.OutOfStock = products.Count(p => p.Quantity == 0);
            ViewBag.LowStock = products.Count(p => p.Quantity > 0 && p.Quantity < 10);
            
            return View(products);
        }

        // POST: Reports/DeleteSale with password verification
        [HttpPost]
        public async Task<IActionResult> DeleteSale([FromBody] DeleteSaleRequest request)
        {
            try
            {
                if (request == null)
                {
                    return Json(new { success = false, message = "অনুরোধ খালি" });
                }
                
                int id = request.Id;
                string password = request.Password ?? "";
                
                // Get the current logged-in user
                var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
                
                if (currentUser == null)
                {
                    System.Console.WriteLine($"❌ Current user not found: {User.Identity?.Name}");
                    return Json(new { success = false, message = "ব্যবহারকারী খুঁজে পাওয়া যায়নি" });
                }

                // Check if user has Admin role
                if (!User.IsInRole("Admin"))
                {
                    return Json(new { success = false, message = "শুধুমাত্র প্রশাসকরা বিক্রয় মুছে ফেলতে পারেন" });
                }

                if (string.IsNullOrEmpty(currentUser.PasswordHash))
                {
                    System.Console.WriteLine($"❌ User {currentUser.Username} has no password hash!");
                    return Json(new { success = false, message = "ব্যবহারকারী পাসওয়ার্ড সেট করা নেই" });
                }

                // Verify the entered password against the user's stored hash
                bool isPasswordValid = VerifyPassword(password, currentUser.PasswordHash);
                
                System.Console.WriteLine($"User: {currentUser.Username}, Password valid: {isPasswordValid}");
                if (!isPasswordValid)
                {
                    System.Console.WriteLine($"Stored hash: {currentUser.PasswordHash.Substring(0, Math.Min(30, currentUser.PasswordHash.Length))}...");
                    System.Console.WriteLine($"Entered password: {password}");
                }

                if (!isPasswordValid)
                {
                    return Json(new { success = false, message = "পাসওয়ার্ড ভুল। বিক্রয় মুছে ফেলা যায়নি" });
                }

                // Check if there are at least 2 admin users before deletion
                var adminUserCount = await _context.Users.CountAsync(u => u.Role == "Admin" && u.IsActive);
                if (adminUserCount < 2)
                {
                    return Json(new { success = false, message = "সিস্টেমে কমপক্ষে একটা সক্রিয় প্রশাসক রাখতে হবে" });
                }

                // Find and delete the sale
                var sale = await _context.SalesTransactions
                    .Include(s => s.SalesItems)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (sale == null)
                {
                    return Json(new { success = false, message = "বিক্রয় রেকর্ড খুঁজে পাওয়া যায়নি" });
                }

                // Also restore the product quantities
                if (sale.SalesItems != null)
                {
                    foreach (var item in sale.SalesItems)
                    {
                        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == item.ProductId);
                        if (product != null)
                        {
                            product.Quantity += item.QuantitySold;
                        }
                    }
                }

                // Remove the sales items and transaction
                _context.SalesItems.RemoveRange(sale.SalesItems ?? new List<SalesItem>());
                _context.SalesTransactions.Remove(sale);
                await _context.SaveChangesAsync();

                System.Diagnostics.Debug.WriteLine($"Sale {id} deleted successfully");
                return Json(new { success = true, message = "বিক্রয় সফলভাবে মুছে ফেলা হয়েছে এবং স্টক পুনরুদ্ধার করা হয়েছে" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in DeleteSale: {ex.Message}");
                return Json(new { success = false, message = $"ত্রুটি: {ex.Message}" });
            }
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        private static bool VerifyPassword(string password, string hash)
        {
            return HashPassword(password) == hash;
        }
    }
}
