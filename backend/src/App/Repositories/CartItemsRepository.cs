using App.Database;
using App.Models;

namespace App.Repositories;


public interface ICartItemsRepository : IBaseRepository<CartItem>
{
}


public class CartItemsRepository(AppDbContext context) : BaseRepository<CartItem>(context), ICartItemsRepository
{
    private readonly AppDbContext _context = context;
}