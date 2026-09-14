namespace App.DTOs;


public class CategoryCreateDto
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}


public class CategoryUpdateDto
{
    public string? Title { get; set; }
    public string? Slug { get; set; }
}


public class CategoryResponseDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
