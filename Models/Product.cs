using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryTracker.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "পণ্যের নাম")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "বিবরণ")]
        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "মূল্য")]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Price { get; set; }

        [Required]
        [Display(Name = "স্টকে পরিমাণ")]
        public int Quantity { get; set; }

        [Display(Name = "ক্যাটাগরি")]
        [StringLength(50)]
        public string? Category { get; set; }

        [Display(Name = "তৈরির তারিখ")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // নেভিগেশন প্রপার্টি
        public ICollection<SalesItem>? SalesItems { get; set; }
    }
}
