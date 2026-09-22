using App.Repositories;
using App.Transactions;

namespace App.Services;


public interface INotificationService
{
    
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
}