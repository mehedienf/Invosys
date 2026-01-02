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
        [HttpGet]
        public async Task<IActionResult> Index(string? searchTerm = "")
        {
            var transactions = await _context.SalesTransactions
                .Include(s => s.SalesItems!)
                .ThenInclude(si => si.Product)
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();
            
            searchTerm = searchTerm?.Trim() ?? "";
            
            if (!string.IsNullOrEmpty(searchTerm))
            {
                transactions = transactions
                    .Where(t => t.CustomerName!.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            
            ViewBag.SearchTerm = searchTerm;
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
        public async Task<IActionResult> Create(string? customerName, string? phoneNumber, string? notes, int[] productIds, int[] quantities)
        {
            if (productIds == null || productIds.Length == 0 || quantities == null || quantities.Length == 0)
            {
                ModelState.AddModelError("", "অন্তত একটি পণ্য নির্বাচন করুন");
                ViewBag.Products = await _context.Products.Where(p => p.Quantity > 0).ToListAsync();
                return View();
            }

            if (productIds.Length != quantities.Length)
            {
                ModelState.AddModelError("", "পণ্য এবং পরিমাণের সংখ্যা মেলে না");
                ViewBag.Products = await _context.Products.Where(p => p.Quantity > 0).ToListAsync();
                return View();
            }

            var transaction = new SalesTransaction
            {
                CustomerName = customerName,
                PhoneNumber = phoneNumber,
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

            // Reload transaction with items and products
            var savedTransaction = await _context.SalesTransactions
                .Include(s => s.SalesItems!)
                .ThenInclude(si => si.Product)
                .FirstOrDefaultAsync(s => s.Id == transaction.Id);

            // Receipt data pass through TempData
            TempData["ReceiptId"] = savedTransaction!.Id;
            TempData["ReceiptDate"] = savedTransaction.SaleDate.ToString("dd/MM/yyyy HH:mm");
            TempData["ReceiptCustomer"] = savedTransaction.CustomerName ?? "অতিথি";
            TempData["ReceiptPhone"] = savedTransaction.PhoneNumber ?? "N/A";
            TempData["ReceiptItems"] = System.Text.Json.JsonSerializer.Serialize(
                savedTransaction.SalesItems!.Select(si => new { 
                    Name = si.Product!.Name, 
                    Qty = si.QuantitySold, 
                    Price = si.UnitPrice,
                    Total = si.QuantitySold * si.UnitPrice
                }).ToList()
            );
            TempData["ReceiptTotal"] = savedTransaction.TotalAmount.ToString("0.00");

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

        // POST: Sales/DeleteMultiple
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMultiple([FromBody] int[] saleIds)
        {
            if (saleIds == null || saleIds.Length == 0)
            {
                return BadRequest("No sales selected");
            }

            try
            {
                var salesToDelete = await _context.SalesTransactions
                    .Include(s => s.SalesItems!)
                    .ThenInclude(si => si.Product)
                    .Where(s => saleIds.Contains(s.Id))
                    .ToListAsync();

                foreach (var sale in salesToDelete)
                {
                    // Restore product quantities
                    if (sale.SalesItems != null)
                    {
                        foreach (var item in sale.SalesItems)
                        {
                            if (item.Product != null)
                            {
                                item.Product.Quantity += item.QuantitySold;
                                _context.Update(item.Product);
                            }
                        }
                    }

                    _context.SalesTransactions.Remove(sale);
                }

                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error deleting sales: {ex.Message}");
            }
        }
    }
}
