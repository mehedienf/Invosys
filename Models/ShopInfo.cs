namespace InventoryTracker.Models;

public class ShopInfo
{
    public int Id { get; set; }
    public string ShopName { get; set; }
    public string Phone { get; set; }
    public string Email { get; set; }
    public string Address { get; set; }
    public string OpeningHours { get; set; }
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
