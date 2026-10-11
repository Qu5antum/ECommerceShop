using App.Database;
using App.DTOs;
using App.Models;
using Microsoft.EntityFrameworkCore;

namespace App.Repositories;


public interface IReviewRepository : IBaseRepository<Review>
{
    Task<List<ProductReviewsResponseDto>> GetReviewsByProductId(Guid productId);
    Task<Review?> GetReviewByUserAndProductId(Guid userId, Guid productId);
}


public class ReviewRepository(AppDbContext context) : BaseRepository<Review>(context), IReviewRepository
{
    private readonly AppDbContext _context = context;

    public async Task<List<ProductReviewsResponseDto>> GetReviewsByProductId(Guid productId)
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
            .ToListAsync();
    }

    public async Task<Review?> GetReviewByUserAndProductId(Guid userId, Guid productId)
    {
        return await _context.Reviews
            .Where(r => r.UserId == userId && r.ProductId == productId)
            .FirstOrDefaultAsync();
    }
}