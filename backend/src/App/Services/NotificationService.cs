using App.DTOs;
using App.Repositories;
using App.Services.Caching;
using App.Transactions;

namespace App.Services;


public interface INotificationService
{
    Task<List<NotificationResponseDto>> GetNotificationsAsync(Guid userId, int? take = null, bool? isRead = null);
    Task<int> GetCountOfUnreadNotificationsAsync(Guid userId);
    Task MarkAllAsReadAsync(Guid userId);
}


public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ILogger<NotificationService> _logger;
    private readonly IHelperService _helper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRedisCacheService _cache;

    private static string GetNotificationsCacheKey(Guid userId, int? take, bool? isRead)
    {
        return $"notifications:{userId}:take:{take}:isRead:{isRead}";
    }

    private static string GetUnreadCountCacheKey(Guid userId)
    {
        return $"notifications:{userId}:unread-count";
    }

    public NotificationService(
        INotificationRepository notificationRepository, 
        ILogger<NotificationService> logger, 
        IHelperService helper, 
        IUnitOfWork unitOfWork,
        IRedisCacheService cache)
    {
        _notificationRepository = notificationRepository;
        _logger = logger;
        _helper = helper;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }
    

    public async Task<List<NotificationResponseDto>> GetNotificationsAsync(Guid userId, int? take = null, bool? isRead = null)
    {
        await _helper.GetUserOr404(userId);

        var cacheKey = GetNotificationsCacheKey(userId, take, isRead);

        var cached = await _cache.GetDataAsync<List<NotificationResponseDto>>(cacheKey);

        if (cached is not null)
        {
            _logger.LogInformation("Notifications retrieved from Redis: {UserId}", userId);
            return cached;
        }

        var notifications = await _notificationRepository.GetNotificationsAsync(userId, take, isRead);

        var result = notifications.Select(notification => new NotificationResponseDto
        {
            Id = notification.Id,
            UserId = notification.UserId,
            Title = notification.Title,
            Message = notification.Message,
            Type = notification.Type,
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt,
            UpdatedAt = notification.UpdatedAt
        }).ToList();

        await _cache.SetDataAsync(
            cacheKey,
            result,
            TimeSpan.FromMinutes(1)
        );

        return result;
    }

    public async Task<int> GetCountOfUnreadNotificationsAsync(Guid userId)
    {
        await _helper.GetUserOr404(userId);

        var cacheKey = GetUnreadCountCacheKey(userId);

        var cachedNotificationsCountUnread = await _cache.GetDataAsync<int?>(cacheKey);

        if (cachedNotificationsCountUnread.HasValue)
        {
            _logger.LogInformation("Notification retrieved from redis cache: {userId}", userId);
            return cachedNotificationsCountUnread.Value;
        }

        int notificationsCount = await _notificationRepository.GetCountOfUnReadNotifications(userId);

        await _cache.SetDataAsync(
            cacheKey,
            notificationsCount,
            TimeSpan.FromMinutes(1)
        );

        _logger.LogInformation("Successfull response of notifications count: {userId}", userId);

        return notificationsCount;
    }

    public async Task MarkAllAsReadAsync(Guid userId)
    {
        await _unitOfWork.BeginTransactionAsync();
        await _helper.GetUserOr404(userId);

        await _notificationRepository.MarkAllAsReadAsync(userId);
        await _unitOfWork.SaveChangesAsync();
        await _unitOfWork.CommitAsync();

        await _cache.RemoveDataAsync(GetUnreadCountCacheKey(userId));
        
        _logger.LogInformation("All notifications marked as read for user: {userId}", userId);
    }
}