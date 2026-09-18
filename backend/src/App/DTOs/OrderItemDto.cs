namespace App.DTOs;


public class OrderItemResponseDto
{
    public Guid Id { get; set; }
    public Guid orderId { get; set; }
    public Guid productId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}