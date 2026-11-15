using App.Models;


public class ProductTestHelper
{
    public static Product CreateTestProduct(
        Guid? Id = null,
        Guid? SellerProfileId = null,
        Guid? CategoryId = null,
        string Name = "TestProductName",
        string Description = "TestProductDescription",
        decimal Price = 500,
        string ImageUrl = "uploads/stores/default-test.jpg",
        string SKU = "121212",
        int Stock = 10)
    {
        return new Product
        {
            Id = Id ?? Guid.NewGuid(),
            SellerProfileId = SellerProfileId ?? Guid.NewGuid(),
            CategoryId = CategoryId ?? Guid.NewGuid(),
            Name = Name,
            Description = Description,
            Price = Price,
            ImageUrl = ImageUrl,
            SKU = SKU,
            Stock = Stock
        };
    }
}