using App.Enum;
using App.Models;


public class PaymentTestHelper
{
    public static Payment CreateTestPayment(
        Guid? Id = null,
        Guid? OrderId = null,
        decimal Amount = 10,
        PaymentStatus paymentStatus = PaymentStatus.Pending,
        string? ProviderPaymentId = null)
    {
        return new Payment
        {
            Id = Id ?? Guid.NewGuid(),
            OrderId = OrderId ?? Guid.NewGuid(),
            Amount = Amount,
            Status = paymentStatus,
            ProviderPaymentId = ProviderPaymentId ?? $"mock_{Guid.NewGuid()}"
        };
    }
}