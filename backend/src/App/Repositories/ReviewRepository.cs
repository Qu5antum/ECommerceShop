using App.Database;
using App.Models;
using Microsoft.EntityFrameworkCore;

namespace App.Repositories;


public interface IReviewRepository : IBaseRepository<Review>
{
    Task<List<Review>> GetReviewsByProductId(Guid productId);
    Task<Review?> GetReviewByUserAndProductId(Guid userId, Guid productId);
}


public class ReviewRepository(AppDbContext context) : BaseRepository<Review>(context), IReviewRepository
{
    private readonly AppDbContext _context = context;

    public async Task<List<Review>> GetReviewsByProductId(Guid productId)
    {
        return await _context.Reviews
            .Where(r => r.ProductId == productId)
            .ToListAsync();
    }

    public async Task<Review?> GetReviewByUserAndProductId(Guid userId, Guid productId)
    {
        return await _context.Reviews
            .Where(r => r.UserId == userId && r.ProductId == productId)
            .FirstOrDefaultAsync();
    }
}