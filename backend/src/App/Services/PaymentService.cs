using App.Repositories;

namespace App.Services;


public interface IPaymentService
{
    
}


public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(IPaymentRepository paymentRepository, IOrderRepository orderRepository, ILogger<PaymentService> logger)
    {
        _paymentRepository = paymentRepository;
        _orderRepository = orderRepository;
        _logger = logger;
    }
}