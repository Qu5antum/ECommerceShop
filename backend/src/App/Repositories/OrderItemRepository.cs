using App.Database;
using App.Models;

namespace App.Repositories;


public interface IOrderItemRepository : IBaseRepository<OrderItem>
{

}


public class OrderItemRepository(AppDbContext context) : BaseRepository<OrderItem>(context), IOrderItemRepository
{

}