using App.Database;
using App.Models;
using Microsoft.EntityFrameworkCore;

namespace App.Repositories;


public interface ISellerProfileRepository : IBaseRepository<SellerProfile>
{
    Task<SellerProfile?> GetSellerProfileByUserIdAsync(Guid userId);
    Task<bool> IsStoreNameTakenAsync(string storeName);
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
}