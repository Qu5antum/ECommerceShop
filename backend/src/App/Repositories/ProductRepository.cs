using App.Database;
using App.DTOs;
using App.Models;
using Microsoft.EntityFrameworkCore;

namespace App.Repositories;


public interface IProductRepository : IBaseRepository<Product>
{
    Task<IEnumerable<Product>> GetProductsByCategoryIdAsync(Guid categoryId, int limit = 50, int offset = 0);
    Task<IEnumerable<Product>> SearchProductByNameAsync(string Name);
    Task<List<Product>> SearchProductByPriceDesc(string ProductName);
    Task<List<Product>> SearchProductByPriceAsc(string ProductName);
    Task<List<Product>> GetProductsOutOfStockAsync(Guid sellerId);
    Task<List<Product>> GetProductsOfSellerAsync(Guid sellerId);
    Task<Guid> GetUserIdByProductId(Guid productId);
    Task<List<ProductResponseDto>> GetProductsWithPagination(int limit = 50, int offset = 0);
}


public class ProductRepository(AppDbContext context) : BaseRepository<Product>(context), IProductRepository
{
    private readonly AppDbContext _context = context;

    public async Task<IEnumerable<Product>> GetProductsByCategoryIdAsync(Guid categoryId, int limit = 50, int offset = 0)
    {
        return await _context.Products
            .AsNoTracking()
            .OrderBy(p => p.Id)
            .Where(p => p.CategoryId == categoryId)
            .Skip(offset)
            .Take(limit)
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

    public async Task<List<Product>> GetProductsOutOfStockAsync(Guid sellerId)
    {
        return await _context.Products
            .AsNoTracking()
            .Where(p => p.Stock == 0 && p.SellerProfileId == sellerId)
            .ToListAsync();
    }

    public async Task<List<Product>> GetProductsOfSellerAsync(Guid sellerId)
    {
        return await _context.Products
            .AsNoTracking()
            .Where(p => p.SellerProfileId == sellerId)
            .ToListAsync();
    }

    public async Task<Guid> GetUserIdByProductId(Guid productId)
    {
        return await _context.Products
            .Where(p => p.Id == productId)
            .Select(p => p.SellerProfile.userId)
            .FirstOrDefaultAsync();
    }

    public async Task<List<ProductResponseDto>> GetProductsWithPagination(int limit = 50, int offset = 0)
    {
        return await _context.Products
            .AsNoTracking()
            .OrderBy(p => p.Id) 
            .Skip(offset)
            .Take(limit)
            .Select(p => new ProductResponseDto 
            {
                Id = p.Id,
                SellerProfileId = p.SellerProfileId,
                CategoryId = p.CategoryId,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                SKU = p.SKU,
                Stock = p.Stock,
                ImageUrl = p.ImageUrl,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .ToListAsync();
    }
}