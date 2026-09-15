using App.Database;
using App.Models;

namespace App.Repositories;


public interface IProductRepository : IBaseRepository<Product>
{
}


public class ProductRepository(AppDbContext context) : BaseRepository<Product>(context), IProductRepository
{
    private readonly AppDbContext _context = context;
}