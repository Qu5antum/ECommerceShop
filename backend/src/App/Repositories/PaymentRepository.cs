using App.Database;
using App.Models;
using Microsoft.EntityFrameworkCore;

namespace App.Repositories;


public interface IPaymentRepository : IBaseRepository<Payment>
{
    Task<Payment?> GetPaymentWithProviderId(string paymentProviderId);
}


public class PaymentRepository(AppDbContext context) : BaseRepository<Payment>(context), IPaymentRepository
{
    private readonly AppDbContext _context = context;

    public async Task<Payment?> GetPaymentWithProviderId(string paymentProviderId)
    {
        return await _context.Payments
            .FirstOrDefaultAsync(p => p.ProviderPaymentId == paymentProviderId);
    }
}