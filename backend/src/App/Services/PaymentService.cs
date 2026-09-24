using App.DTOs;
using App.Enum;
using App.Exceptions;
using App.Models;
using App.Repositories;
using App.Services.Caching;
using App.Transactions;
using Microsoft.EntityFrameworkCore;

namespace App.Services;


public interface IPaymentService
{
    Task<PaymentResponseDto> CreatePaymentAsync(Guid userId, Guid orderId);
    Task<PaymentResponseDto> GetPaymentAsync(Guid userId, Guid orderId, Guid paymentId);
    Task<bool> ProcessWebhookAsync(PaymentWebhookDto webhookDto);
}


public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IHelperService _helper;
    private readonly ILogger<PaymentService> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRedisCacheService _cache;

    private static string GetPaymentCacheKey(Guid paymentId)
    {
        return $"payment:{paymentId}";
    }

    public PaymentService(
        IPaymentRepository paymentRepository, 
        INotificationRepository notificationRepository, 
        IHelperService helper, 
        ILogger<PaymentService> logger, 
        IUnitOfWork unitOfWork,
        IRedisCacheService cache
    )
    {
        _paymentRepository = paymentRepository;
        _notificationRepository = notificationRepository;
        _helper = helper;
        _logger = logger;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<PaymentResponseDto> CreatePaymentAsync(Guid userId, Guid orderId)
    {
        await _unitOfWork.BeginTransactionAsync();
        await _helper.GetUserOr404(userId);

        var order = await _helper.GetOrderOr404(orderId);

        if (order.userId != userId)
        {
            _logger.LogWarning("Order does not belong to user, user ID: {userId}, order ID: {orderId}", userId, orderId);
            throw new BadRequestException("Order does not belong to user");
        }

        if (order.status == OrderStatus.Cancelled)
        {
            _logger.LogWarning("Bad request, order cancelled, can't pay order: {orderId}", orderId);
            throw new BadRequestException("Can't pay for order, order cancelled");
        }

        if (order.status == OrderStatus.Paid)
        {
            _logger.LogWarning("Bad request, Order already in paid status: {orderId}", orderId);
            throw new BadRequestException("Order already paid");
        }

        string ProviderPaymentId = $"mock_{Guid.NewGuid()}";

        try
        {
            var newPayment = new Payment
            {
                OrderId = orderId,
                Amount = order.TotalAmount,
                Status = PaymentStatus.Pending,
                ProviderPaymentId = ProviderPaymentId
            };

            await _paymentRepository.CreateAsync(newPayment);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            return new PaymentResponseDto
            {
                Id = newPayment.Id,
                OrderId = newPayment.OrderId,
                Amount = newPayment.Amount,
                Status = newPayment.Status,
                ProviderPaymentId = newPayment.ProviderPaymentId,
                CreatedAt = newPayment.CreatedAt,
                UpdatedAt = newPayment.UpdatedAt
            };
        }
        catch (DbUpdateException ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError(ex, "A database error occurred while creating the payment: {Message}.", ex.Message);
            throw new DatabaseException("Could not save the payment to the database.");
        }
    }
    
    public async Task<PaymentResponseDto> GetPaymentAsync(Guid userId, Guid orderId, Guid paymentId)
    {
        await _helper.GetUserOr404(userId);

        var cacheyKey = GetPaymentCacheKey(paymentId);

        var cachedPayment = await _cache.GetDataAsync<PaymentResponseDto>(cacheyKey);

        if (cachedPayment is not null)
        {
            _logger.LogInformation("Payment retrieved from redis cache, user ID: {userId}, payment ID: {paymentId}", userId, paymentId);
            return cachedPayment;
        }

        var order = await _helper.GetOrderOr404(orderId);

        if (order.userId != userId)
        {
            _logger.LogWarning("Order does not belong to user, user ID: {userId}, order ID: {orderId}", userId, orderId);
            throw new BadRequestException("Order does not belong to user");
        }

        var payment = await _helper.GetPaymentOr404(paymentId);

        if (payment.OrderId != orderId)
        {
            _logger.LogWarning("Payment does not belong to order, payment ID: {paymentId}, order ID: {orderId}", paymentId, orderId);
            throw new BadRequestException("Payment does not belong to order");
        }
        
        var result = new PaymentResponseDto
        {
            Id = payment.Id,
            OrderId = payment.OrderId,
            Amount = payment.Amount,
            Status = payment.Status,
            ProviderPaymentId = payment.ProviderPaymentId,
            CreatedAt = payment.CreatedAt,
            UpdatedAt = payment.UpdatedAt
        };

        await _cache.SetDataAsync(
            cacheyKey,
            result,
            TimeSpan.FromMinutes(5)
        );

        _logger.LogInformation("Successfull response of payment of user: {userId}", userId);

        return result;
    }

    public async Task<bool> ProcessWebhookAsync(PaymentWebhookDto webhookDto)
    {
        await _unitOfWork.BeginTransactionAsync();

        var payment = await _paymentRepository.GetPaymentWithProviderId(webhookDto.ProviderPaymentId);

        if (payment == null)
        {
            _logger.LogWarning("Payment not found for provider payment ID: {providerId}", webhookDto.ProviderPaymentId);
            throw new NotFoundException("Payment not found");
        }

        if (payment.Status == PaymentStatus.Succeeded)
        {
            _logger.LogInformation("Payment already marked as Succeeded. Provider ID: {providerId}", webhookDto.ProviderPaymentId);
            return true; 
        }

        var order = await _helper.GetOrderOr404(payment.OrderId);

        try
        {
            payment.Status = webhookDto.Status;
            payment.UpdatedAt = DateTime.UtcNow;

            string notificationTitle = string.Empty;
            string notificationMessage = string.Empty;
            NotificationType notificationType = NotificationType.General;

            if (webhookDto.Status == PaymentStatus.Succeeded)
            {
                order.status = OrderStatus.Paid;
                order.UpdatedAt = DateTime.UtcNow;

                _logger.LogInformation("Order status updated to Paid via webhook. Order ID: {orderId}", order.Id);

                notificationTitle = "Payment successful";
                notificationMessage = $"Your payment of {payment.Amount} for order #{order.Id} was successful.";
                notificationType = NotificationType.PaymentSuccess;
            }
            else if (webhookDto.Status == PaymentStatus.Failed)
            {
                order.status = OrderStatus.Pending; 
                order.UpdatedAt = DateTime.UtcNow;
                
                _logger.LogWarning("Payment failed via webhook for order ID: {orderId}", order.Id);

                notificationTitle = "Payment failed";
                notificationMessage = $"Payment for order #{order.Id} has failed. Please try again.";
                notificationType = NotificationType.PaymentFailed;
            }

            var notification = new Notification
            {
                UserId = order.userId, 
                Title = notificationTitle,
                Message = notificationMessage,
                Type = notificationType,
                IsRead = false
            };
            
            await _notificationRepository.CreateAsync(notification);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Payment status updated: {paymentId}", payment.Id);

            await _cache.RemoveDataAsync(GetPaymentCacheKey(payment.Id));

            _logger.LogInformation("Payment deleted from redis cache: {paymentId}", payment.Id);

            return true;
        }
        catch (DbUpdateException ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError(ex, "A database error occurred while processing webhook: {Message}.", ex.Message);
            throw new DatabaseException("Could not process payment webhook in the database.");
        }
    }
}