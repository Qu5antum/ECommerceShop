using System.ComponentModel.DataAnnotations;
using App.Enum;

namespace App.DTOs;


public class PaymentWebhookDto
{
    [Required]
    public string ProviderPaymentId { get; set; } = string.Empty;

    [Required]
    public PaymentStatus Status { get; set; }
}