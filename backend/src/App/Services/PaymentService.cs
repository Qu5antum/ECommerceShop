using App.DTOs;
using App.Enum;
using App.Exceptions;
using App.Models;
using App.Repositories;
using App.Transactions;
using Microsoft.EntityFrameworkCore;

namespace App.Services;


public interface IPaymentService
{
    Task<PaymentResponseDto> CreatePaymentAsync(Guid userId, Guid orderId);
    Task<PaymentResponseDto> GetPaymentAsync(Guid userId, Guid orderId, Guid paymentId);
}


public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IHelperService _helper;
    private readonly ILogger<PaymentService> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public PaymentService(IPaymentRepository paymentRepository, IHelperService helper, ILogger<PaymentService> logger, IUnitOfWork unitOfWork)
    {
        _paymentRepository = paymentRepository;
        _helper = helper;
        _logger = logger;
        _unitOfWork = unitOfWork;
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
        
        return new PaymentResponseDto
        {
            Id = payment.Id,
            OrderId = payment.OrderId,
            Amount = payment.Amount,
            Status = payment.Status,
            ProviderPaymentId = payment.ProviderPaymentId,
            CreatedAt = payment.CreatedAt,
            UpdatedAt = payment.UpdatedAt
        };
    }
}