using App.Models;


public class CartItemTestHelper
{
    public static CartItem CreateTestCartItem(
        Guid? Id = null,
        Guid? CartId = null,
        Guid? ProductId = null,
        int Quantity = 5)
    {
        return new CartItem
        {
            Id = Id ?? Guid.NewGuid(),
            CartId = CartId ?? Guid.NewGuid(),
            ProductId = ProductId ?? Guid.NewGuid(),
            Quantity = Quantity
        };
    }
}