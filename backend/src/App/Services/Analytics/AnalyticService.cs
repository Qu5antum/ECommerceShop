using App.DTOs;
using App.Repositories;
using App.Services.Caching;

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
    private readonly IRedisCacheService _cache;

    private static string GetGeneralAnalyticsCacheKey()
    {
        return $"analytics:general:all";
    }

    public AnalyticService(
        IUserRepository userRepository,
        ISellerProfileRepository sellerRepository,
        IProductRepository productRepository,
        IOrderRepository orderRepository,
        IPaymentRepository paymentRepository,
        IReviewRepository reviewRepository,
        ILogger<AnalyticService> logger,
        IRedisCacheService cache)
    {
        _userRepository = userRepository;
        _sellerRepository = sellerRepository;
        _productRepository = productRepository;
        _orderRepository = orderRepository;
        _paymentRepository = paymentRepository;
        _reviewRepository = reviewRepository;
        _logger = logger;
        _cache = cache;
    }

    public async Task<GeneralAnalyticsResponseDto> GetGeneralAnalyticsAdminAsync()
    {
        var cacheKey = GetGeneralAnalyticsCacheKey();

        var cachedGeneralAnalytics = await _cache.GetDataAsync<GeneralAnalyticsResponseDto>(cacheKey);

        if (cachedGeneralAnalytics is not null)
        {
            _logger.LogInformation("General analytics retrieved from redis cache");
            return cachedGeneralAnalytics;
        }

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

        await _cache.SetDataAsync(
            cacheKey, 
            result,
            TimeSpan.FromMinutes(10)
        );

        _logger.LogInformation("Successfull response of general analytics");

        return result;
    }
}