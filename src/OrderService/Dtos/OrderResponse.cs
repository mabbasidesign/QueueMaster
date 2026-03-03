namespace OrderService.Dtos;

public record OrderResponse(
    int Id,
    string CustomerName,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal TotalAmount,
    string Status,
    DateTime CreatedAtUtc
);
