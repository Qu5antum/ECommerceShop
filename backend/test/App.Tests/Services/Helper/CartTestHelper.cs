using App.Models;


public class CartTestHelper
{
    public static Cart CreateTestHelper(
        Guid? Id = null,
        Guid? UserId = null)
    {
        return new Cart
        {
            Id = Id ?? Guid.NewGuid(),
            UserId = UserId ?? Guid.NewGuid()
        };
    }
}