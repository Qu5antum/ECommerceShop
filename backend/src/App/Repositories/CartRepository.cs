using App.Database;
using App.Models;
using Microsoft.EntityFrameworkCore;

namespace App.Repositories;


public interface ICartRepository : IBaseRepository<Cart>
{
    Task<Cart?> GetCartWithItemsByUserIdAsync(Guid userId);
    Task<Guid?> GetCartIdByUserId(Guid userId);
}


public class CartRepository(AppDbContext context) : BaseRepository<Cart>(context), ICartRepository
{
    private readonly AppDbContext _context = context;

    public async Task<Cart?> GetCartWithItemsByUserIdAsync(Guid userId)
    {
        return await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);
    }

    public async Task<Guid?> GetCartIdByUserId(Guid userId)
    {
        return await _context.Carts
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync();
    }
}