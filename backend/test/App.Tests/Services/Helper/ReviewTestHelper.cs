using App.Models;


public class ReviewTestHelper
{
    public  static Review CreateTestReview(
        Guid? Id = null,
        Guid? UserId = null,
        Guid? ProductId = null,
        int Rating = 5,
        string Comment = "TestComment")
    {
        return new Review
        {
            Id = Id ?? Guid.NewGuid(),
            UserId = UserId ?? Guid.NewGuid(),
            ProductId = ProductId ?? Guid.NewGuid(),
            Rating = Rating,
            Comment = Comment
        };
    }
}