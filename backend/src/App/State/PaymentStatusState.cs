using App.Enum;
using App.Exceptions;

namespace App.State;


public static class PaymentStatusState
{
    private static readonly Dictionary<PaymentStatus, HashSet<PaymentStatus>> AllowedTransitions = new()
    {
        [PaymentStatus.Pending] = new() { PaymentStatus.Succeeded, PaymentStatus.Failed, PaymentStatus.Refunded },
        [PaymentStatus.Succeeded] = new() { PaymentStatus.Refunded }, 
        [PaymentStatus.Failed] = new() { PaymentStatus.Pending, PaymentStatus.Succeeded },
        [PaymentStatus.Refunded] = new()
    };

    public static bool CanTransitionTo(PaymentStatus currentStatus, PaymentStatus newStatus)
    {
        return AllowedTransitions.TryGetValue(currentStatus, out var allowed) 
               && allowed.Contains(newStatus);
    }

    public static void ValidateTransition(PaymentStatus currentStatus, PaymentStatus newStatus)
    {
        if (currentStatus == newStatus)
        {
            throw new BadRequestException($"Payment is already in {currentStatus} status.");
        }

        if (!CanTransitionTo(currentStatus, newStatus))
        {
            throw new BadRequestException($"Invalid payment status transition from {currentStatus} to {newStatus}.");
        }
    }
}