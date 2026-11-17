using App.Enum;
using App.Exceptions;

namespace App.State;


public static class OrderStatusState
{
    private static readonly Dictionary<OrderStatus, HashSet<OrderStatus>> AllowedTransitions = new()
    {
        [OrderStatus.Pending] = new() { OrderStatus.PaymentPending, OrderStatus.Paid, OrderStatus.Cancelled },
        [OrderStatus.PaymentPending] = new() { OrderStatus.Paid, OrderStatus.Cancelled },
        [OrderStatus.Paid] = new() { OrderStatus.Processing, OrderStatus.Cancelled },
        [OrderStatus.Processing] = new() { OrderStatus.Shipped, OrderStatus.Cancelled },
        [OrderStatus.Shipped] = new() { OrderStatus.Delivered }, 
        [OrderStatus.Delivered] = new(),                         
        [OrderStatus.Cancelled] = new()                          
    };

    public static bool CanTransitionTo(OrderStatus currentStatus, OrderStatus newStatus)
    {
        return AllowedTransitions.TryGetValue(currentStatus, out var allowed) 
               && allowed.Contains(newStatus);
    }

    public static void ValidateTransition(OrderStatus currentStatus, OrderStatus newStatus)
    {
        if (currentStatus == OrderStatus.Cancelled || currentStatus == OrderStatus.Delivered)
        {
            throw new BadRequestException($"Cannot change status of a {currentStatus} order.");
        }

        if (newStatus == OrderStatus.Cancelled && (currentStatus == OrderStatus.Shipped || currentStatus == OrderStatus.Delivered))
        {
            throw new BadRequestException("Cannot cancel an order that has already been shipped or delivered.");
        }

        if (!CanTransitionTo(currentStatus, newStatus))
        {
            throw new BadRequestException("Order status transition is not available");
        }
    }
}