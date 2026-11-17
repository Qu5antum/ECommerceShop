using App.Models;


public class OrderItemTestHelper
{
    public static OrderItem CreateTestOrderItem(
        Guid? Id = null,
        Guid? orderId = null,
        Guid? productId = null,
        string? ProductName = null,
        decimal? Price = null,
        int Quantity = 5)
    {
        return new OrderItem
        {
            Id = Id ?? Guid.NewGuid(),
            orderId = orderId ?? Guid.NewGuid(),
            productId = productId ?? Guid.NewGuid(),
            ProductName = ProductName ?? "TestProductName",
            Price = Price ?? 500,
            Quantity = Quantity
        };
    }
}