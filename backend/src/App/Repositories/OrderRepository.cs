using App.Database;
using App.Models;
using Microsoft.EntityFrameworkCore;

namespace App.Repositories;


public interface IOrderRepository : IBaseRepository<Order>
{
    Task<IEnumerable<Order>> GetOrdersByUserIdAsync(Guid userId);
    Task<Order?> GetOrderWithItemsById(Guid orderId);
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
}