using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryTracker.Models
{
    public class SalesTransaction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "বিক্রয়ের তারিখ")]
        public DateTime SaleDate { get; set; } = DateTime.Now;

        [Display(Name = "গ্রাহকের নাম")]
        [StringLength(100)]
        public string? CustomerName { get; set; }

        [Display(Name = "মোট মূল্য")]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalAmount { get; set; }

        [Display(Name = "মন্তব্য")]
        [StringLength(500)]
        public string? Notes { get; set; }

        // নেভিগেশন প্রপার্টি
        public ICollection<SalesItem>? SalesItems { get; set; }
    }
}
