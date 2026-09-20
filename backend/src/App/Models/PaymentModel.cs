using App.Enum;

namespace App.Models;


public class Payment : BaseModel
{
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; }
    public string? ProviderPaymentId { get; set; } 
}