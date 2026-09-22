using App.Database;
using App.Models;
using Microsoft.EntityFrameworkCore;

namespace App.Repositories;


public interface IProductRepository : IBaseRepository<Product>
{
    Task<IEnumerable<Product>> GetProductsByCategoryIdAsync(Guid categoryId);
    Task<IEnumerable<Product>> SearchProductByNameAsync(string Name);
    Task<List<Product>> GetProductsByMultipleIds(List<Guid> productIds);
    Task<List<Product>> SearchProductByPriceDesc(string ProductName);
    Task<List<Product>> SearchProductByPriceAsc(string ProductName);
}


public class ProductRepository(AppDbContext context) : BaseRepository<Product>(context), IProductRepository
{
    private readonly AppDbContext _context = context;

    public async Task<IEnumerable<Product>> GetProductsByCategoryIdAsync(Guid categoryId)
    {
        return await _context.Products
            .AsNoTracking()
            .Where(p => p.CategoryId == categoryId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Product>> SearchProductByNameAsync(string Name)
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            return new List<Product>();
        }

        return await _context.Products
            .AsNoTracking()
            .Where(p => p.Name.ToLower().Contains(Name.ToLower()))
            .ToListAsync();
    }

    public async Task<List<Product>> GetProductsByMultipleIds(List<Guid> productIds)
    {
        return await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync();
    }

    public async Task<List<Product>> SearchProductByPriceDesc(string ProductName)
    {
        return await _context.Products
            .AsNoTracking()
            .Where(p => p.Name.ToLower().Contains(ProductName.ToLower()))
            .OrderByDescending(p => p.Price)
            .ToListAsync();
    }

    public async Task<List<Product>> SearchProductByPriceAsc(string ProductName)
    {
        return await _context.Products
            .AsNoTracking()
            .Where(p => p.Name.ToLower().Contains(ProductName.ToLower()))
            .OrderBy(p => p.Price)
            .ToListAsync();
    }
}