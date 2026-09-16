namespace App.Models;


public class Cart : BaseModel
{
    public Guid UserId { get; set; }
    public List<CartItem> Items { get; set; } = new();
}