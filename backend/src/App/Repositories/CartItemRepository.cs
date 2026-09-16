using App.Database;
using App.Models;
using Microsoft.EntityFrameworkCore;

namespace App.Repositories;


public interface ICartItemRepository : IBaseRepository<CartItem>
{
    Task<IEnumerable<CartItem>> GetAllItemsInCartAsyncByCartId(Guid cartId);
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
}