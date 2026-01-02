using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using InventoryTracker.Data;
using InventoryTracker.Models;
using Microsoft.AspNetCore.Authorization;

namespace InventoryTracker.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Account/Login
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == model.Username && u.IsActive);

            if (user == null || !VerifyPassword(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError("", "ভুল ইউজারনেম বা পাসওয়ার্ড");
                return View(model);
            }

            // Create claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.GivenName, user.FullName),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        // GET: Account/Logout
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        // GET: Account/AccessDenied
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // GET: Account/Users (Admin only)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Users()
        {
            var users = await _context.Users.OrderBy(u => u.Username).ToListAsync();
            return View(users);
        }

        // GET: Account/Register (Admin only)
        [Authorize(Roles = "Admin")]
        public IActionResult Register()
        {
            return View();
        }

        // POST: Account/Register (Admin only)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Check if username exists
            if (await _context.Users.AnyAsync(u => u.Username == model.Username))
            {
                ModelState.AddModelError("Username", "এই ইউজারনেম আগে থেকে আছে");
                return View(model);
            }

            var user = new User
            {
                Username = model.Username,
                PasswordHash = HashPassword(model.Password),
                FullName = model.FullName,
                Role = model.Role,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"ইউজার '{model.Username}' সফলভাবে তৈরি হয়েছে";
            return RedirectToAction(nameof(Users));
        }

        // GET: Account/Edit/5 (Admin only)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Protect system admin from editing
            if (user.Username == "system")
            {
                TempData["Error"] = "সিস্টেম অ্যাডমিন সম্পাদনা করা যায় না";
                return RedirectToAction(nameof(Users));
            }

            var model = new RegisterViewModel
            {
                Username = user.Username,
                FullName = user.FullName,
                Role = user.Role
            };

            ViewBag.UserId = user.Id;
            ViewBag.IsActive = user.IsActive;
            return View(model);
        }

        // POST: Account/Edit/5 (Admin only)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RegisterViewModel model, bool isActive)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Protect system admin from editing
            if (user.Username == "system")
            {
                TempData["Error"] = "সিস্টেম অ্যাডমিন সম্পাদনা করা যায় না";
                return RedirectToAction(nameof(Users));
            }

            // Check if username exists for other users
            if (await _context.Users.AnyAsync(u => u.Username == model.Username && u.Id != id))
            {
                ModelState.AddModelError("Username", "এই ইউজারনেম আগে থেকে আছে");
                ViewBag.UserId = id;
                ViewBag.IsActive = isActive;
                return View(model);
            }

            user.Username = model.Username;
            user.FullName = model.FullName;
            user.Role = model.Role;
            user.IsActive = isActive;

            // Only update password if provided
            if (!string.IsNullOrEmpty(model.Password))
            {
                user.PasswordHash = HashPassword(model.Password);
            }

            _context.Update(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "ইউজার আপডেট হয়েছে";
            return RedirectToAction(nameof(Users));
        }

        // POST: Account/Delete/5 (Admin only)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                // Protect system admin from deletion
                if (user.Username == "system")
                {
                    TempData["Error"] = "সিস্টেম অ্যাডমিন মুছে ফেলা যায় না";
                    return RedirectToAction(nameof(Users));
                }

                // Don't delete yourself
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (currentUserId == id.ToString())
                {
                    TempData["Error"] = "আপনি নিজেকে মুছতে পারবেন না";
                    return RedirectToAction(nameof(Users));
                }

                // Check if this is the last active admin user
                if (user.Role == "Admin" && user.IsActive)
                {
                    var activeAdminCount = await _context.Users.CountAsync(u => u.Role == "Admin" && u.IsActive);
                    if (activeAdminCount <= 1)
                    {
                        TempData["Error"] = "সিস্টেমে কমপক্ষে একটা সক্রিয় প্রশাসক রাখতে হবে";
                        return RedirectToAction(nameof(Users));
                    }
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                TempData["Success"] = "ইউজার মুছে ফেলা হয়েছে";
            }
            return RedirectToAction(nameof(Users));
        }

        // Password hashing
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

        // GET: Account/ShopInfo (Admin and Staff)
        [Authorize]
        public async Task<IActionResult> ShopInfo()
        {
            // Get shop info from database, or use default values
            var shopInfo = await _context.ShopInfos.FirstOrDefaultAsync();
            
            if (shopInfo == null)
            {
                // Default values if no shop info exists
                shopInfo = new ShopInfo
                {
                    ShopName = "আমাদের দোকান",
                    Phone = "+880 1XXX-XXXXXX",
                    Email = "info@shop.com",
                    Address = "ঢাকা, বাংলাদেশ",
                    OpeningHours = "সোম - রবি, ৯ AM - ৯ PM",
                    Description = "আমরা উচ্চমানের পণ্য সরবরাহ করি এবং সর্বোত্তম গ্রাহক সেবা প্রদান করি।"
                };
            }
            
            return View(shopInfo);
        }

        // GET: Account/EditShopInfo (Admin only)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditShopInfo()
        {
            var shopInfo = await _context.ShopInfos.FirstOrDefaultAsync();
            
            if (shopInfo == null)
            {
                // Create default if doesn't exist
                shopInfo = new ShopInfo
                {
                    ShopName = "আমাদের দোকান",
                    Phone = "+880 1XXX-XXXXXX",
                    Email = "info@shop.com",
                    Address = "ঢাকা, বাংলাদেশ",
                    OpeningHours = "সোম - রবি, ৯ AM - ৯ PM",
                    Description = "আমরা উচ্চমানের পণ্য সরবরাহ করি এবং সর্বোত্তম গ্রাহক সেবা প্রদান করি.",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                _context.ShopInfos.Add(shopInfo);
                await _context.SaveChangesAsync();
            }
            
            return View(shopInfo);
        }

        // POST: Account/EditShopInfo (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditShopInfo(ShopInfo model)
        {
            if (ModelState.IsValid)
            {
                var shopInfo = await _context.ShopInfos.FirstOrDefaultAsync();
                
                if (shopInfo == null)
                {
                    // Create new if doesn't exist
                    shopInfo = new ShopInfo
                    {
                        ShopName = model.ShopName,
                        Phone = model.Phone,
                        Email = model.Email,
                        Address = model.Address,
                        OpeningHours = model.OpeningHours,
                        Description = model.Description,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };
                    _context.ShopInfos.Add(shopInfo);
                }
                else
                {
                    // Update existing
                    shopInfo.ShopName = model.ShopName;
                    shopInfo.Phone = model.Phone;
                    shopInfo.Email = model.Email;
                    shopInfo.Address = model.Address;
                    shopInfo.OpeningHours = model.OpeningHours;
                    shopInfo.Description = model.Description;
                    shopInfo.UpdatedAt = DateTime.Now;
                }
                
                await _context.SaveChangesAsync();
                TempData["Success"] = "দোকানের তথ্য সফলভাবে আপডেট করা হয়েছে";
                return RedirectToAction(nameof(ShopInfo));
            }
            
            return View(model);
        }
    }
}
