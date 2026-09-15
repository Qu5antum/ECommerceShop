namespace App.DTOs;


public class ProductCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public IFormFile? Image { get; set; }
    public string SKU { get; set; } = string.Empty;
    public int Stock { get; set; }
}


public class ProductUpdateDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public IFormFile? Image { get; set; }
    public string? SKU { get; set; }
    public int? Stock { get; set; }
}


public class ProductResponseDto
{
    public Guid Id { get; set; }
    public Guid SellerId { get; set; }
    public Guid CategoryId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public string SKU { get; set; } = string.Empty;
    public int Stock { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}