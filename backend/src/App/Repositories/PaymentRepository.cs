using App.Database;
using App.Models;

namespace App.Repositories;


public interface IPaymentRepository : IBaseRepository<Payment>
{
    
}


public class PaymentRepository(AppDbContext context) : BaseRepository<Payment>(context), IPaymentRepository
{
    private readonly AppDbContext _context = context;
}