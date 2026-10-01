using App.Database;
using App.Models;
using Microsoft.EntityFrameworkCore;

namespace App.Repositories;


public interface IBaseRepository<T> where T : BaseModel
{
    Task<T> CreateAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
    Task<T?> GetByIdAsync(Guid id);
    Task<List<T>> GetAllAsync();
    Task AddRangeAsync(IEnumerable<T> entities);
    Task<int> GetEntitysCountAsync();
}


public class BaseRepository<T>(AppDbContext context) : IBaseRepository<T> where T : BaseModel
{
    private readonly DbSet<T> _dbSet = context.Set<T>();

    public async Task<T> CreateAsync(T entity)
    {
        await _dbSet.AddAsync(entity);

        return entity;
    }

    public async Task UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
    }

    public async Task DeleteAsync(T entity)
    {
        _dbSet.Remove(entity);
    }

    public async Task<T?> GetByIdAsync(Guid id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<List<T>> GetAllAsync()
    {
        return await _dbSet.AsNoTracking().ToListAsync();
    }

    public async Task AddRangeAsync(IEnumerable<T> entities)
    {
        if (entities == null || !entities.Any()) return;

        await _dbSet.AddRangeAsync(entities);
    }

    public async Task<int> GetEntitysCountAsync()
    {
        return await _dbSet
            .Select(e => e.Id)
            .CountAsync();
    }
}