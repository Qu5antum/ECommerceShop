using App.Database;
using App.Models;

namespace App.Repositories;


public interface IOrderRepository : IBaseRepository<Order>
{
    
}


public class OrderRepository(AppDbContext context) : BaseRepository<Order>(context), IOrderRepository
{
    private readonly AppDbContext _context = context;
}