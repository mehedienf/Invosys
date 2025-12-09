using System.ComponentModel.DataAnnotations;

namespace InventoryTracker.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "ইউজারনেম")]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [Display(Name = "পাসওয়ার্ড")]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [Display(Name = "পুরো নাম")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "রোল")]
        [StringLength(20)]
        public string Role { get; set; } = "Staff"; // Admin বা Staff

        [Display(Name = "সক্রিয়")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "তৈরির তারিখ")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    // Login ViewModel
    public class LoginViewModel
    {
        [Required(ErrorMessage = "ইউজারনেম দিন")]
        [Display(Name = "ইউজারনেম")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "পাসওয়ার্ড দিন")]
        [DataType(DataType.Password)]
        [Display(Name = "পাসওয়ার্ড")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "মনে রাখুন")]
        public bool RememberMe { get; set; }
    }

    // Register ViewModel (Admin only)
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "ইউজারনেম দিন")]
        [Display(Name = "ইউজারনেম")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "ইউজারনেম ৩-৫০ অক্ষরের হতে হবে")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "পাসওয়ার্ড দিন")]
        [DataType(DataType.Password)]
        [Display(Name = "পাসওয়ার্ড")]
        [StringLength(100, MinimumLength = 4, ErrorMessage = "পাসওয়ার্ড কমপক্ষে ৪ অক্ষরের হতে হবে")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "পাসওয়ার্ড নিশ্চিত করুন")]
        [DataType(DataType.Password)]
        [Display(Name = "পাসওয়ার্ড নিশ্চিত করুন")]
        [Compare("Password", ErrorMessage = "পাসওয়ার্ড মিলছে না")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "পুরো নাম দিন")]
        [Display(Name = "পুরো নাম")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "রোল সিলেক্ট করুন")]
        [Display(Name = "রোল")]
        public string Role { get; set; } = "Staff";
    }
}
