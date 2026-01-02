using Microsoft.EntityFrameworkCore;
using InventoryTracker.Models;

namespace InventoryTracker.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // আপনার টেবিলগুলো (DB Sets)
        public DbSet<Product> Products { get; set; }
        public DbSet<SalesTransaction> SalesTransactions { get; set; }
        public DbSet<SalesItem> SalesItems { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<ShopInfo> ShopInfos { get; set; }
     }
}