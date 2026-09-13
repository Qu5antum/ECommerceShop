using App.Database;
using App.Enum;
using App.Models;
using Microsoft.EntityFrameworkCore;

namespace App.Repositories;


public interface IUserRepository : IBaseRepository<User>
{
    Task<User?> GetUserByEmail(string Email);
    Task<User?> GetUserByUserName(string UserName);
    Task<List<User>> GetAllUsersNotAdminAsync();
}


public class UserRepository(AppDbContext context) : BaseRepository<User>(context), IUserRepository
{
    private readonly AppDbContext _context = context;

    public async Task<User?> GetUserByEmail(string Email)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == Email);

        if (user == null)
        {
            return null;
        }

        return user;
    }

    public async Task<User?> GetUserByUserName(string UserName)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == UserName);

        if (user == null)
        {
            return null;
        }

        return user;
    }

    public async Task<List<User>> GetAllUsersNotAdminAsync()
    {
        var nonAdminUsers = await _context.Users
            .Where(u => (u.Roles & UserRole.Admin) == 0)
            .ToListAsync();

        return nonAdminUsers;
    }
}