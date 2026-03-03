using PaymentService.Models;

namespace PaymentService.Services;

public interface IPaymentService
{
    Task<Payment?> GetPaymentByTransactionIdAsync(Guid transactionId);
    Task<List<Payment>> GetPaymentsByOrderIdAsync(int orderId);
    Task<List<Payment>> GetAllPaymentsAsync();
    Task<Payment> CreatePaymentAsync(Payment payment);
    Task<Payment> UpdatePaymentAsync(Payment payment);
    Task<bool> DeletePaymentAsync(Guid transactionId);
}
