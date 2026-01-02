using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using InventoryTracker.Data;
using InventoryTracker.Models;

namespace InventoryTracker.Controllers
{
    [Authorize(Roles = "Admin")]
    public class InventoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InventoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Inventory
        public async Task<IActionResult> Index(string searchTerm = "")
        {
            var products = await _context.Products.ToListAsync();
            searchTerm = searchTerm?.Trim() ?? "";
            
            if (!string.IsNullOrEmpty(searchTerm))
            {
                products = products
                    .Where(p => p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                                p.Category.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            
            ViewBag.SearchTerm = searchTerm;
            return View(products);
        }

        // GET: Inventory/Adjust/5
        public async Task<IActionResult> Adjust(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Inventory/Adjust/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Adjust(int id, int adjustmentQuantity, string adjustmentType)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            if (adjustmentType == "add")
            {
                product.Quantity += adjustmentQuantity;
            }
            else if (adjustmentType == "subtract")
            {
                product.Quantity -= adjustmentQuantity;
                if (product.Quantity < 0) product.Quantity = 0;
            }

            _context.Update(product);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
