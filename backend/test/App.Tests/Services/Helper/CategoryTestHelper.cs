using App.Models;


public class CategoryTestHelper
{
    public static Category CreateTestCategory(
        string Title = "TestCategory",
        string Slug = "TestSlug")
    {
        return new Category
        {
            Title = Title,
            Slug = Slug,
        };
    }
}