using App.Enum;

namespace App.Models;


public class Order : BaseModel
{
    public Guid userId { get; set; }
    public User User { get; set; } = null!;
    public decimal TotalAmount { get; set; }
    public OrderStatus status { get; set; } = OrderStatus.Pending;
    public List<OrderItem> orderItems { get; set; } = new List<OrderItem>();
}