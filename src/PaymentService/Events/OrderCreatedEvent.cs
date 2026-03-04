namespace PaymentService.Events;

public record OrderCreatedEvent(
    int OrderId,
    string CustomerName,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal TotalAmount,
    DateTime CreatedAtUtc
);
