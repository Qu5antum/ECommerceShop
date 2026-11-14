using App.Enum;
using App.Models;


public static class SellerProfileTestHelper
{
    public static SellerProfile CreateTestSellerProfile(
        Guid? Id = null,
        Guid? userId = null,
        string storeName = "TestStoreName",
        string description = "TestDescription",
        string imageUrl = "uploads/stores/default-test.jpg",
        SellerStatus status = SellerStatus.Pending)
    {
        return new SellerProfile
        {
            Id = Id ?? Guid.NewGuid(),
            userId = userId ?? Guid.NewGuid(),
            StoreName = storeName,
            Description = description,
            ImageUrl = imageUrl,
            Status = status,
        };
    }
}