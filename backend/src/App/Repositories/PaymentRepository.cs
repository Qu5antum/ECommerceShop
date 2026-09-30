using App.Database;
using App.DTOs;
using App.Enum;
using App.Models;
using Microsoft.EntityFrameworkCore;

namespace App.Repositories;


public interface IPaymentRepository : IBaseRepository<Payment>
{
    Task<Payment?> GetPaymentWithProviderId(string paymentProviderId);
    Task<List<Payment>> GetPaymetsByStatusAsync(PaymentStatus? status = null);
    Task<PaymentStatisticsDto> GetPaymentsStatisticsAsync();
}


public class PaymentRepository(AppDbContext context) : BaseRepository<Payment>(context), IPaymentRepository
{
    private readonly AppDbContext _context = context;

    public async Task<Payment?> GetPaymentWithProviderId(string paymentProviderId)
    {
        return await _context.Payments
            .FirstOrDefaultAsync(p => p.ProviderPaymentId == paymentProviderId);
    }

    public async Task<List<Payment>> GetPaymetsByStatusAsync(PaymentStatus? status = null)
    {
        var query = _context.Payments
            .AsNoTracking();

        if (status.HasValue) 
            query = query.Where(p => p.Status == status);
        
        var result = await query.ToListAsync();

        return result;
    }

    public async Task<PaymentStatisticsDto> GetPaymentsStatisticsAsync()
    {
        var stats = await _context.Payments
            .GroupBy(p => 1)
            .Select(g => new PaymentStatisticsDto
            {
                Total = g.Count(),
                Successfull = g.Count(p => p.Status == PaymentStatus.Succeeded),
                Failed = g.Count(p => p.Status == PaymentStatus.Failed),
                Pending = g.Count(p => p.Status == PaymentStatus.Pending),
                Refunded = g.Count(p => p.Status == PaymentStatus.Refunded),
                Revenue = g.Where(p => p.Status == PaymentStatus.Succeeded)
                    .Sum(p => (decimal?)p.Amount) ?? 0m
            })
            .FirstOrDefaultAsync();
        return stats ?? new PaymentStatisticsDto();
    }
}