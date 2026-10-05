using App.Enum;

namespace App.Models;


public class SellerProfile : BaseModel
{
    public Guid userId { get; set; }
    public User User { get; set; } = null!;

    public string StoreName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public SellerStatus Status { get; set; } = SellerStatus.Pending;
    public ICollection<Product> Products { get; set; } = new List<Product>();
}