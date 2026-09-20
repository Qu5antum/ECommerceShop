using App.Enum;

namespace App.DTOs;


public class SellerOrderItemResponseDto
{
    public Guid OrderId { get; set; }
    public OrderStatus OrderStatus { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public decimal TotalItemPrice => Price * Quantity;
}