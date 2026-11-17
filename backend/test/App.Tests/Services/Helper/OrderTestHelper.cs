using App.Enum;
using App.Models;


public class OrderTestHelper
{
    public static Order CreateTestOrder(
        Guid? Id = null,
        Guid? userId = null,
        decimal TotalAmount = 500,
        OrderStatus status = OrderStatus.Pending)
    {
        return new Order
        {
            Id = Id ?? Guid.NewGuid(),
            userId = userId ?? Guid.NewGuid(),
            TotalAmount = TotalAmount,
            status = status
        };
    }
}