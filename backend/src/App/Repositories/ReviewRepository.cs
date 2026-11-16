using App.Database;
using App.DTOs;
using App.Models;
using Microsoft.EntityFrameworkCore;

namespace App.Repositories;


public interface IReviewRepository : IBaseRepository<Review>
{
    Task<List<ProductReviewsResponseDto>> GetReviewsByProductId(Guid productId, int limit = 50, int offset = 0);
    Task<Review?> GetReviewByUserAndProductId(Guid userId, Guid productId);
    Task<List<ReviewResponseDto>> GetReviewsWithPagination(int limit = 50, int offset = 0);
}


public class ReviewRepository(AppDbContext context) : BaseRepository<Review>(context), IReviewRepository
{
    private readonly AppDbContext _context = context;

    public async Task<List<ProductReviewsResponseDto>> GetReviewsByProductId(Guid productId, int limit = 50, int offset = 0)
    {
        return await _context.Reviews
            .Where(r => r.ProductId == productId)
            .Select(r => new ProductReviewsResponseDto
            {
                Id = r.Id,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
                User = r.User == null ? null : new UserReviewPreviewResponseDto
                {
                    Id = r.User.Id,
                    UserName = r.User.UserName
                }
            })
            .Skip(offset)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<Review?> GetReviewByUserAndProductId(Guid userId, Guid productId)
    {
        return await _context.Reviews
            .Where(r => r.UserId == userId && r.ProductId == productId)
            .FirstOrDefaultAsync();
    }

    public async Task<List<ReviewResponseDto>> GetReviewsWithPagination(int limit = 50,  int offset = 0)
    {
        limit = Math.Clamp(limit, 1, 100);
        offset = Math.Max(0, offset);

        return await _context.Reviews
            .AsNoTracking()
            .OrderByDescending(r => r.CreatedAt)
            .Skip(offset)                      
            .Take(limit)                      
            .Select(r => new ReviewResponseDto 
            {
                Id = r.Id,
                UserId = r.UserId,
                ProductId = r.ProductId,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            })
            .ToListAsync();
    }
}