using Microsoft.EntityFrameworkCore;
using PaymentService.Data;
using PaymentService.Models;

namespace PaymentService.Services;

public class PaymentService : IPaymentService
{
    private readonly PaymentDbContext _context;

    public PaymentService(PaymentDbContext context)
    {
        _context = context;
    }

    public async Task<Payment?> GetPaymentByTransactionIdAsync(Guid transactionId)
    {
        return await _context.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.TransactionId == transactionId);
    }

    public async Task<List<Payment>> GetPaymentsByOrderIdAsync(int orderId)
    {
        return await _context.Payments.AsNoTracking().Where(p => p.OrderId == orderId).ToListAsync();
    }

    public async Task<List<Payment>> GetAllPaymentsAsync()
    {
        return await _context.Payments.AsNoTracking().ToListAsync();
    }

    public async Task<Payment> CreatePaymentAsync(Payment payment)
    {
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();
        return payment;
    }

    public async Task<Payment> UpdatePaymentAsync(Payment payment)
    {
        _context.Payments.Update(payment);
        await _context.SaveChangesAsync();
        return payment;
    }

    public async Task<bool> DeletePaymentAsync(Guid transactionId)
    {
        var payment = await _context.Payments.FirstOrDefaultAsync(p => p.TransactionId == transactionId);
        if (payment is null)
            return false;

        _context.Payments.Remove(payment);
        await _context.SaveChangesAsync();
        return true;
    }
}
