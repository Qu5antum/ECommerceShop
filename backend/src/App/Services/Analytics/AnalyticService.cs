using App.DTOs;
using App.Repositories;

namespace App.Services.Analtytics;


public interface IAnalyticService
{
    Task<GeneralAnalyticsResponseDto> GetGeneralAnalyticsAdminAsync();
}


public class AnalyticService : IAnalyticService
{
    private readonly IUserRepository _userRepository;
    private readonly ISellerProfileRepository _sellerRepository;
    private readonly IProductRepository _productRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IReviewRepository _reviewRepository;
    private readonly ILogger<AnalyticService> _logger;

    public AnalyticService(
        IUserRepository userRepository,
        ISellerProfileRepository sellerRepository,
        IProductRepository productRepository,
        IOrderRepository orderRepository,
        IPaymentRepository paymentRepository,
        IReviewRepository reviewRepository,
        ILogger<AnalyticService> logger)
    {
        _userRepository = userRepository;
        _sellerRepository = sellerRepository;
        _productRepository = productRepository;
        _orderRepository = orderRepository;
        _paymentRepository = paymentRepository;
        _reviewRepository = reviewRepository;
        _logger = logger;
    }

    // TODO: add redis cache
    public async Task<GeneralAnalyticsResponseDto> GetGeneralAnalyticsAdminAsync()
    {
        var usersCount = await _userRepository.GetUsersCountAsync();
        var sellersCount = await _sellerRepository.GetEntitysCountAsync();
        var productsCount = await _productRepository.GetEntitysCountAsync();
        var ordersCount = await _orderRepository.GetEntitysCountAsync();
        var reviewsCount = await _reviewRepository.GetEntitysCountAsync();
        var pendingOrdersCount = await _orderRepository.GetPendingOrdersCountAsync();
        var pendingPaymentsCount = await _paymentRepository.GetPendingPaymentsCountAsync();
        var totalRevenue = await _paymentRepository.GetTotalRevenueAsync();

        var result = new GeneralAnalyticsResponseDto
        {
            Users = usersCount,
            Sellers = sellersCount,
            Products = productsCount,
            Orders = ordersCount,
            PendingOrders = pendingOrdersCount,
            PendingPayments = pendingPaymentsCount,
            Revenue = totalRevenue,
            Reviews = reviewsCount,
        };

        _logger.LogInformation("Successfull response of general analytics");

        return result;
    }
}