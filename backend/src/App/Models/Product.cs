namespace App.Models;


public class Product : BaseModel
{
    public Guid SellerProfileId { get; set; }
    public SellerProfile SellerProfile { get; set; } = null!;

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public string SKU { get; set; } = string.Empty;
    public int Stock { get; set; }
}