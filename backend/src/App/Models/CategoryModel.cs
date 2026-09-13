namespace App.Models;


public class Category : BaseModel
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}