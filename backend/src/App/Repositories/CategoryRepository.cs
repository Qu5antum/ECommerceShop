using App.Database;
using App.Models;
using Microsoft.EntityFrameworkCore;

namespace App.Repositories;


public interface ICategoryRepsitory : IBaseRepository<Category>
{
    Task<Category?> GetCategoryByTitle(string Title);
    Task<Category?> GetCategoryBySlug(string Slug);
}


public class CategoryRepository(AppDbContext context) : BaseRepository<Category>(context), ICategoryRepsitory
{
    private readonly AppDbContext _context = context;

    public async Task<Category?> GetCategoryByTitle(string Title)
    {
        var category = await _context.Categories.FirstOrDefaultAsync(c => c.Title == Title);

        if (category == null)
        {
            return null;
        }

        return category;
    }

    public async Task<Category?> GetCategoryBySlug(string Slug)
    {
        var category = await _context.Categories.FirstOrDefaultAsync(c => c.Slug == Slug);

        if (category == null)
        {
            return null;
        }

        return category;
    }
}