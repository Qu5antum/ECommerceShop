using App.Database;
using App.DTOs;
using App.Enum;
using App.Models;
using Microsoft.EntityFrameworkCore;

namespace App.Repositories;


public interface ISellerProfileRepository : IBaseRepository<SellerProfile>
{
    Task<SellerProfile?> GetSellerProfileByUserIdAsync(Guid userId);
    Task<bool> IsStoreNameTakenAsync(string storeName);
    Task<List<SellerProfile>> GetSellersByStatusAsync(SellerStatus status);
    Task<SellerPreviewResponseDto?> GetSellerStoreNameDescriptionAsync(Guid sellerId);
    Task<Guid> GetUserIdBySellerProfileId(Guid sellerId);
}


public class SellerProfileRepository(AppDbContext context) : BaseRepository<SellerProfile>(context), ISellerProfileRepository
{
    private readonly AppDbContext _context = context;

    public async Task<SellerProfile?> GetSellerProfileByUserIdAsync(Guid userId)
    {
        return await _context.SellerProfiles
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.userId == userId);
    }

    public async Task<bool> IsStoreNameTakenAsync(string storeName)
    {
        return await _context.SellerProfiles
            .AnyAsync(s => s.StoreName == storeName);
    }

    public async Task<List<SellerProfile>> GetSellersByStatusAsync(SellerStatus status)
    {
        return await _context.SellerProfiles
            .AsNoTracking()
            .Where(s => s.Status == status)
            .ToListAsync();
    }

    public async Task<SellerPreviewResponseDto?> GetSellerStoreNameDescriptionAsync(Guid sellerId)
    {
        return await _context.SellerProfiles
            .Where(s => s.Id == sellerId)
            .Select(s => new SellerPreviewResponseDto
            {
                StoreName = s.StoreName,
                Description = s.Description
            })
            .FirstOrDefaultAsync();
    }

    public async Task<Guid> GetUserIdBySellerProfileId(Guid sellerId)
    {
        return await _context.SellerProfiles
            .Where(s => s.Id == sellerId)
            .Select(s => s.userId)
            .FirstOrDefaultAsync();
    }
}