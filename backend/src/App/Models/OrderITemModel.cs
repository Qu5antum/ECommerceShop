namespace App.Models;


public class OrderItem : BaseModel
{
    public Guid orderId { get; set; }
    public Order Order { get; set; } = null!;

    public Guid productId { get; set; }
    public Product Product { get; set; } = null!;

    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}