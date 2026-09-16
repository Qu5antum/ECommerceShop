namespace App.Models;


public class CartItem : BaseModel
{
    public Guid CartId { get; set; }
    public Cart Cart { get; set; } = null!;
    
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}