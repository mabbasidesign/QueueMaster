namespace PaymentService.Dtos;

public record ProcessPaymentRequest(
    int OrderId,
    decimal Amount,
    string Currency,
    string Method
);
