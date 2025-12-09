using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using InventoryTracker.Data;
using InventoryTracker.Models;

namespace InventoryTracker.Controllers
{
    [Authorize]
    public class SalesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SalesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Sales
        public async Task<IActionResult> Index()
        {
            var transactions = await _context.SalesTransactions
                .Include(s => s.SalesItems!)
                .ThenInclude(si => si.Product)
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();
            return View(transactions);
        }

        // GET: Sales/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Products = await _context.Products.Where(p => p.Quantity > 0).ToListAsync();
            return View();
        }

        // POST: Sales/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string? customerName, string? notes, int[] productIds, int[] quantities)
        {
            if (productIds == null || productIds.Length == 0)
            {
                ModelState.AddModelError("", "অন্তত একটি পণ্য নির্বাচন করুন");
                ViewBag.Products = await _context.Products.Where(p => p.Quantity > 0).ToListAsync();
                return View();
            }

            var transaction = new SalesTransaction
            {
                CustomerName = customerName,
                Notes = notes,
                SaleDate = DateTime.Now,
                SalesItems = new List<SalesItem>()
            };

            decimal totalAmount = 0;

            for (int i = 0; i < productIds.Length; i++)
            {
                var product = await _context.Products.FindAsync(productIds[i]);
                if (product != null && quantities[i] > 0 && product.Quantity >= quantities[i])
                {
                    var saleItem = new SalesItem
                    {
                        ProductId = product.Id,
                        QuantitySold = quantities[i],
                        UnitPrice = product.Price
                    };

                    transaction.SalesItems.Add(saleItem);
                    totalAmount += product.Price * quantities[i];

                    // স্টক থেকে কমাও
                    product.Quantity -= quantities[i];
                    _context.Update(product);
                }
            }

            transaction.TotalAmount = totalAmount;

            _context.SalesTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Sales/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var transaction = await _context.SalesTransactions
                .Include(s => s.SalesItems!)
                .ThenInclude(si => si.Product)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (transaction == null)
            {
                return NotFound();
            }

            return View(transaction);
        }
    }
}
