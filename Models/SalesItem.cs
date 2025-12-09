using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryTracker.Models
{
    public class SalesItem
    {
        [Key]
        public int Id { get; set; }

        // ফরেন কী (Foreign Key)
        public int TransactionId { get; set; }
        // নেভিগেশন প্রপার্টি
        public SalesTransaction? Transaction { get; set; }

        // ফরেন কী (Foreign Key)
        public int ProductId { get; set; }
        // নেভিগেশন প্রপার্টি
        public Product? Product { get; set; }

        [Required]
        [Display(Name = "বিক্রিত পরিমাণ")]
        public int QuantitySold { get; set; }

        [Required]
        [Display(Name = "ইউনিট মূল্য")]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal UnitPrice { get; set; }
    }
}