using App.Database;
using App.Models;

namespace App.Repositories;


public interface IReviewRepository : IBaseRepository<Review>
{

}


public class ReviewRepository(AppDbContext context) : BaseRepository<Review>(context), IReviewRepository
{
    private readonly AppDbContext _context = context;
}