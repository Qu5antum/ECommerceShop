using App.Database;
using App.Models;
using Microsoft.EntityFrameworkCore;

namespace App.Repositories;


public interface IOrderItemRepository : IBaseRepository<OrderItem>
{
    Task<IEnumerable<OrderItem>> GetOrderItemsBySellerIdAsync(Guid sellerProfileId);
}


public class OrderItemRepository(AppDbContext context) : BaseRepository<OrderItem>(context), IOrderItemRepository
{
    private readonly AppDbContext _context = context;

    public async Task<IEnumerable<OrderItem>> GetOrderItemsBySellerIdAsync(Guid sellerProfileId)
    {
        return await _context.OrderItems
            .Include(oi => oi.Order)
            .ThenInclude(o => o.User)
            .Include(oi => oi.Product)
            .Where(oi => oi.Product.SellerProfileId == sellerProfileId)
            .ToListAsync();
    }
}