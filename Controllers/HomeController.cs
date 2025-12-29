using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using InventoryTracker.Models;
using InventoryTracker.Data;

namespace InventoryTracker.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        // Dashboard statistics
        ViewBag.TotalProducts = await _context.Products.CountAsync();
        ViewBag.TotalStock = await _context.Products.SumAsync(p => p.Quantity);
        ViewBag.TotalSales = await _context.SalesTransactions.CountAsync();
        
        var today = DateTime.Today;
        // SQLite doesn't support Sum on decimal, so we fetch data and aggregate on client side
        var todaySales = await _context.SalesTransactions
            .Where(s => s.SaleDate.Date == today)
            .Select(s => s.TotalAmount)
            .ToListAsync();
        
        ViewBag.TodaySales = todaySales.Sum();
        
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
