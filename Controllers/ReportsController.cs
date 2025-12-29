using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using InventoryTracker.Data;
using InventoryTracker.Models;

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
                .Select(s => s.TotalAmount)
                .ToListAsync();
            ViewBag.TotalRevenue = revenues.Sum();
            
            // Low stock products
            ViewBag.LowStockProducts = await _context.Products
                .Where(p => p.Quantity < 10)
                .OrderBy(p => p.Quantity)
                .ToListAsync();
            
            // Top selling products
            ViewBag.TopProducts = await _context.SalesItems
                .GroupBy(si => si.ProductId)
                .Select(g => new {
                    ProductId = g.Key,
                    TotalSold = g.Sum(x => x.QuantitySold),
                    TotalRevenue = g.Sum(x => x.QuantitySold * x.UnitPrice)
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(5)
                .ToListAsync();
            
            // Recent sales
            ViewBag.RecentSales = await _context.SalesTransactions
                .OrderByDescending(s => s.SaleDate)
                .Take(10)
                .ToListAsync();
            
            // Monthly sales (last 6 months)
            var sixMonthsAgo = DateTime.Now.AddMonths(-6);
            ViewBag.MonthlySales = await _context.SalesTransactions
                .Where(s => s.SaleDate >= sixMonthsAgo)
                .GroupBy(s => new { s.SaleDate.Year, s.SaleDate.Month })
                .Select(g => new {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    TotalSales = g.Count(),
                    TotalRevenue = g.Sum(x => x.TotalAmount)
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();
            
            return View();
        }

        // GET: Reports/Sales
        public async Task<IActionResult> Sales(DateTime? startDate, DateTime? endDate)
        {
            var query = _context.SalesTransactions
                .Include(s => s.SalesItems!)
                .ThenInclude(si => si.Product)
                .AsQueryable();

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

            var sales = await query.OrderByDescending(s => s.SaleDate).ToListAsync();
            
            ViewBag.TotalSales = sales.Count;
            ViewBag.TotalRevenue = sales.Sum(s => s.TotalAmount);
            
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
    }
}
