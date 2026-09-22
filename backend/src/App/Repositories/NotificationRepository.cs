using App.Database;
using App.Models;
using Microsoft.EntityFrameworkCore;

namespace App.Repositories;


public interface INotificationRepository : IBaseRepository<Notification>
{
    Task<List<Notification>> GetNotificationsAsync(Guid userId);
}


public class NotificationRepository(AppDbContext context) : BaseRepository<Notification>(context), INotificationRepository
{
    private readonly AppDbContext _context = context;

    public async Task<List<Notification>> GetNotificationsAsync(Guid userId)
    {
        return await _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .ToListAsync();
    }
}