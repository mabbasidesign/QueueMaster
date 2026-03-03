namespace PaymentService.Dtos;

public record PaymentResponse(
    Guid TransactionId,
    int OrderId,
    decimal Amount,
    string Currency,
    string Method,
    string Status,
    DateTime CreatedAtUtc
);
