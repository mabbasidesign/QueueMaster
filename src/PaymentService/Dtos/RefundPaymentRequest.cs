namespace PaymentService.Dtos;

public record RefundPaymentRequest(
    decimal Amount,
    string Reason
);
