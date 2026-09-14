namespace App.DTOs;


public class SellerProfileCreateDto
{
    public string StoreName { get; set; } = string.Empty;
    public string? Description { get; set; }
}


public class SellerProfileResponseDto
{
    public Guid Id { get; set; }
    public Guid userId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsApproved { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

}