namespace PaymentService.Models;

public class Payment
{
    public Guid TransactionId { get; set; }
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Method { get; set; } = string.Empty;
    public string Status { get; set; } = "Processing";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
