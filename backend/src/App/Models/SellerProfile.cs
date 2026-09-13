namespace App.Models;


public class SellerProfile : BaseModel
{
    public Guid userId { get; set; }
    public User User { get; set; } = null!;
    
    public string StoreName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
}