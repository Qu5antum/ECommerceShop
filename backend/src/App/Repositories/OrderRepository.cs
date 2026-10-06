using App.Database;
using App.DTOs;
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
    Task<int> GetPendingOrdersCountAsync();
    Task<List<OrderResponseWithOutItemsDto>> GetOrderOfSellerAsync(Guid sellerId);
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

    public async Task<int> GetPendingOrdersCountAsync()
    {
        return await _context.Orders
            .Where(o => o.status == OrderStatus.Pending)
            .Select(o => o.Id)
            .CountAsync();
    }

    public async Task<List<OrderResponseWithOutItemsDto>> GetOrderOfSellerAsync(Guid sellerId)
    {
        return await _context.Orders
            .AsNoTracking()
            .Where(o => o.orderItems.Any(oi => oi.Product.SellerProfileId == sellerId))
            .Select(o => new OrderResponseWithOutItemsDto
            {
                Id = o.Id,
                userId = o.userId,
                TotalAmount = o.TotalAmount,
                status = o.status,
                CreatedAt = o.CreatedAt,
                UpdatedAt = o.UpdatedAt
            })
            .ToListAsync();
    }
}