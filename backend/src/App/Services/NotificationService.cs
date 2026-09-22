using App.DTOs;
using App.Repositories;
using App.Transactions;

namespace App.Services;


public interface INotificationService
{
    Task<List<NotificationResponseDto>> GetNotificationsAsync(Guid userId, int? take = null, bool? isRead = null);
    Task<int> GetCountOfUnreadNotificationsAsync(Guid userId);
}


public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ILogger<NotificationService> _logger;
    private readonly IHelperService _helper;
    private readonly IUnitOfWork _unitOfWork;

    public NotificationService(INotificationRepository notificationRepository, ILogger<NotificationService> logger, IHelperService helper, IUnitOfWork unitOfWork)
    {
        _notificationRepository = notificationRepository;
        _logger = logger;
        _helper = helper;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<NotificationResponseDto>> GetNotificationsAsync(Guid userId, int? take = null, bool? isRead = null)
    {
        await _unitOfWork.BeginTransactionAsync();
        await _helper.GetUserOr404(userId);

        var notifications = await _notificationRepository.GetNotificationsAsync(userId, take, isRead);

        if (isRead == false)
        {
            await _notificationRepository.MarkAllAsReadAsync(userId);
            await _unitOfWork.SaveChangesAsync();
        }

        _logger.LogInformation("Succesfull response of notifications of user: {userId}", userId);

        return notifications.Select(notification => new NotificationResponseDto
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
    }

    public async Task<int> GetCountOfUnreadNotificationsAsync(Guid userId)
    {
        await _helper.GetUserOr404(userId);

        int notificationsCount = await _notificationRepository.GetCountOfUnReadNotifications(userId);

        _logger.LogInformation("Successfull response of notifications count: {userId}", userId);

        return notificationsCount;
    }
}