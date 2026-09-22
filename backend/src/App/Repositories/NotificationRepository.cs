using App.Database;
using App.Models;

namespace App.Repositories;


public interface INotificationRepository : IBaseRepository<Notification>
{
    
}


public class NotificationRepository(AppDbContext context) : BaseRepository<Notification>(context), INotificationRepository
{
    
}