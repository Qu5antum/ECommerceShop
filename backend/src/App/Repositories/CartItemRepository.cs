using App.Database;
using App.Models;
using Microsoft.EntityFrameworkCore;

namespace App.Repositories;


public interface ICartItemRepository : IBaseRepository<CartItem>
{
    Task<IEnumerable<CartItem>> GetAllItemsInCartAsyncByCartId(Guid cartId);
    Task<bool> GetCartItemByProductId(Guid productId);
}


public class CartItemRepository(AppDbContext context) : BaseRepository<CartItem>(context), ICartItemRepository
{
    private readonly AppDbContext _context = context;

    public async Task<IEnumerable<CartItem>> GetAllItemsInCartAsyncByCartId(Guid cartId)
    {
        return await _context.cartItems
            .AsNoTracking()
            .Where(i => i.CartId == cartId)
            .ToListAsync();
    }

    public async Task<bool> GetCartItemByProductId(Guid productId)
    {
        return await _context.cartItems
            .AsNoTracking()
            .AnyAsync(i => i.ProductId == productId);
    }
}