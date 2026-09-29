using App.Database;
using App.Enum;
using App.Models;
using Microsoft.EntityFrameworkCore;

namespace App.Repositories;


public interface IOrderRepository : IBaseRepository<Order>
{
    Task<IEnumerable<Order>> GetOrdersByUserIdAsync(Guid userId);
    Task<Order?> GetOrderWithItemsById(Guid orderId);
    Task<List<Order>> GetOrdersWithStatus(OrderStatus? status = null);
    Task<List<Order>> GetOrderWithDate(DateTime fromDate, DateTime toDate);
}


public class OrderRepository(AppDbContext context) : BaseRepository<Order>(context), IOrderRepository
{
    private readonly AppDbContext _context = context;

    public async Task<IEnumerable<Order>> GetOrdersByUserIdAsync(Guid userId)
    {
        return await _context.Orders
            .Include(o => o.orderItems)
            .Where(o => o.userId == userId)
            .ToListAsync();
    }

    public async Task<Order?> GetOrderWithItemsById(Guid orderId)
    {
        return await _context.Orders
            .Include(o => o.orderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId);
    }

    public async Task<List<Order>> GetOrdersWithStatus(OrderStatus? status = null)
    {
        var query = _context.Orders
            .AsNoTracking();
        
        if (status.HasValue)
        {
            query = query.Where(p => p.status == status.Value);
        }

        var result = await query.ToListAsync();

        return result;
    }

    public async Task<List<Order>> GetOrderWithDate(DateTime fromDate, DateTime toDate)
    {
        var endOfDay = toDate.Date.AddDays(1).AddTicks(-1);

        return await _context.Orders
            .AsNoTracking()
            .Where(o => o.CreatedAt >= fromDate && o.CreatedAt <= endOfDay)
            .ToListAsync();
    }
}