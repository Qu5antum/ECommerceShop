using App.Enum;

namespace App.DTOs;


public class PaymentResponseDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; }
    public string? ProviderPaymentId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}


public class PaymentStatisticsDto
{
    public int Total { get; set; }
    public int Successfull { get; set; }
    public int Failed { get; set; }
    public int Pending { get; set; }
    public int Refunded { get; set; }
    public decimal Revenue { get; set; }
}