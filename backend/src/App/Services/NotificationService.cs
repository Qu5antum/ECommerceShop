using App.DTOs;
using App.Models;
using App.Exceptions;
using App.Repositories;
using App.Services.Caching;
using App.Transactions;
using Microsoft.EntityFrameworkCore;
using App.Enum;

namespace App.Services;


public interface INotificationService
{
    Task<List<NotificationResponseDto>> GetNotificationsAsync(Guid userId, bool? isRead = null, int limit = 50, int offset = 0);
    Task<NotificationResponseDto> GetUserNotificationAsync(Guid userId, Guid notificationId);
    Task<int> GetCountOfUnreadNotificationsAsync(Guid userId);
    Task MarkAllAsReadAsync(Guid userId);
    Task<bool> SendNotificationsAdminAsync(CreateNotificationDto notificationDto);
    Task<List<NotificationResponseDto>> GetAllNotificationsAdminAsync(NotificationType? notificationType, int limit = 50, int offset = 0);
    Task<NotificationResponseDto> GetNotificationAdminAsync(Guid notificationId);
    Task<bool> DeleteNotificationAdminAsync(Guid notificationId);
}


public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<NotificationService> _logger;
    private readonly IHelperService _helper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRedisCacheService _cache;

    private static string GetNotificationsCacheKey(Guid userId, bool? isRead)
    {
        return $"notifications:{userId}:isRead:{isRead}";
    }

    private static string GetUnreadCountCacheKey(Guid userId)
    {
        return $"notifications:{userId}:unread-count";
    }

    private static string GetNotificationsCacheKey()
    {
        return "notifications:all";
    }

    private static string GetNotificationCacheKey(Guid notificationId)
    {
        return $"notifications:{notificationId}";
    }

    private static string GetNotificationUserCacheKey(Guid userId, Guid notificationId)
    {
        return $"notification:user:{userId}:{notificationId}";
    }

    public NotificationService(
        INotificationRepository notificationRepository, 
        IUserRepository userRepository,
        ILogger<NotificationService> logger, 
        IHelperService helper, 
        IUnitOfWork unitOfWork,
        IRedisCacheService cache)
    {
        _notificationRepository = notificationRepository;
        _userRepository = userRepository;
        _logger = logger;
        _helper = helper;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }
    

    public async Task<List<NotificationResponseDto>> GetNotificationsAsync(Guid userId, bool? isRead = null, int limit = 50, int offset = 0)
    {
        await _helper.GetUserOr404(userId);

        var cacheKey = GetNotificationsCacheKey(userId, isRead);

        var cached = await _cache.GetDataAsync<List<NotificationResponseDto>>(cacheKey);

        if (cached is not null)
        {
            _logger.LogInformation("Notifications retrieved from Redis: {UserId}", userId);
            return cached;
        }

        var notifications = await _notificationRepository.GetNotificationsAsync(userId, isRead, limit, offset);

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

    public async Task<NotificationResponseDto> GetUserNotificationAsync(Guid userId, Guid notificationId)
    {
        var cacheKey = GetNotificationUserCacheKey(userId, notificationId);

        var cachedNotification = await _cache.GetDataAsync<NotificationResponseDto>(cacheKey);

        if (cachedNotification is not null)
        {
            _logger.LogInformation("Notification retrieved from redis cache: {notificationId}", notificationId);
            return cachedNotification;
        }

        var notification = await _helper.GetNotificationOr404(notificationId);

        if (notification.UserId != userId)
        {
            _logger.LogInformation("Notification not belong to user, user ID: {userId}, notification ID: {notificationId}", userId, notificationId);
            throw new BadRequestException("Notification not belong to user");
        }

        var result = new NotificationResponseDto
        {
            Id = notification.Id,
            UserId = notification.UserId,
            Title = notification.Title,
            Message = notification.Message,
            Type = notification.Type,
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt,
            UpdatedAt = notification.UpdatedAt
        };

        await _cache.SetDataAsync(
            cacheKey,
            result,
            TimeSpan.FromMinutes(5)
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
        await _cache.RemoveDataAsync(GetNotificationsCacheKey());

        _logger.LogInformation("Notifications deleted from redis cache");
        _logger.LogInformation("All notifications marked as read for user: {userId}", userId);
    }

    public async Task<bool> SendNotificationsAdminAsync(CreateNotificationDto notificationDto)
    {
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            var userIds = await _userRepository.GetUserIdsWithDefaultRolesAsync();

            var notifications = userIds.Select(userId => new Notification
            {
                UserId = userId,
                Title = notificationDto.Title,
                Message = notificationDto.Message,
            }).ToList();

            await _notificationRepository.AddRangeAsync(notifications);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            await _cache.RemoveDataAsync(GetNotificationsCacheKey());

            _logger.LogInformation("Notifications deleted from redis cache");

            _logger.LogInformation("Notifications sended to users, Title: {title}, Message: {message}", notificationDto.Title, notificationDto.Message);

            return true;
        }
        catch (DbUpdateException ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError(ex, "A database error occurred while sending notifications: {Message}.", ex.Message);
            throw new DatabaseException("Could not process sending notifications in the database.");
        }
    }

    public async Task<List<NotificationResponseDto>> GetAllNotificationsAdminAsync(NotificationType? notificationType, int limit = 50, int offset = 0)
    {
        var cacheKey = GetNotificationsCacheKey();

        var cachedNotifications = await _cache.GetDataAsync<List<NotificationResponseDto>>(cacheKey);

        if (cachedNotifications is not null)
        {
            _logger.LogInformation("Notifications retrieved from redis cache");
            return cachedNotifications;
        }

        var notifications = await _notificationRepository.GetAllNotificationWithPagination(notificationType, limit, offset);

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
            TimeSpan.FromMinutes(5)
        );

        return result;
    }

    public async Task<NotificationResponseDto> GetNotificationAdminAsync(Guid notificationId)
    {
        var cacheKey = GetNotificationCacheKey(notificationId);

        var cachedNotification = await _cache.GetDataAsync<NotificationResponseDto>(cacheKey);

        if (cachedNotification is not null)
        {
            _logger.LogInformation("Notification retrieved from redis cache: {notificationId}", notificationId);
            return cachedNotification;
        }

        var notification = await _helper.GetNotificationOr404(notificationId);

        var result = new NotificationResponseDto
        {
            Id = notification.Id,
            UserId = notification.UserId,
            Title = notification.Title,
            Message = notification.Message,
            Type = notification.Type,
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt,
            UpdatedAt = notification.UpdatedAt
        };

        await _cache.SetDataAsync(
            cacheKey,
            result,
            TimeSpan.FromMinutes(5)
        );

        return result;
    }

    public async Task<bool> DeleteNotificationAdminAsync(Guid notificationId)
    {
        await _unitOfWork.BeginTransactionAsync();
        var notification = await _helper.GetNotificationOr404(notificationId);

        try
        {
            await _notificationRepository.DeleteAsync(notification);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            await _cache.RemoveDataAsync(GetNotificationCacheKey(notificationId));
            await _cache.RemoveDataAsync(GetNotificationsCacheKey());

            _logger.LogInformation("Notification deleted from redis cache: {notificationId}", notificationId);
            
            _logger.LogInformation("Notification successfully deleted: {notificationId}", notificationId);

            return true;
        }
        catch (DbUpdateException ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError(ex, "A database error occurred while deleting notifications: {Message}.", ex.Message);
            throw new DatabaseException("Could not process deleting notifications in the database.");
        }
    }
}