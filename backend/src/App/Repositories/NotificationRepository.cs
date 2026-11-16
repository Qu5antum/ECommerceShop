using App.Database;
using App.Models;
using Microsoft.EntityFrameworkCore;

namespace App.Repositories;


public interface INotificationRepository : IBaseRepository<Notification>
{
    Task<List<Notification>> GetNotificationsAsync(Guid userId, bool? isRead = null, int limit = 50, int offset = 0);
    Task<int> GetCountOfUnReadNotifications(Guid userId);
    Task MarkAllAsReadAsync(Guid userId);
}


public class NotificationRepository(AppDbContext context) : BaseRepository<Notification>(context), INotificationRepository
{
    private readonly AppDbContext _context = context;

    public async Task<List<Notification>> GetNotificationsAsync(Guid userId, bool? isRead = null, int limit = 50, int offset = 0)
    {
        var query = _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId);

        if (isRead.HasValue)
        {
            query = query.Where(n => n.IsRead == isRead.Value);
        }

        query = query.OrderByDescending(n => n.CreatedAt);

        return await query.Skip(offset).Take(limit).ToListAsync();
    }

    public async Task<int> GetCountOfUnReadNotifications(Guid userId)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId && n.IsRead == false)
            .CountAsync();
    }

    public async Task MarkAllAsReadAsync(Guid userId)
    {
        await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
    }
}