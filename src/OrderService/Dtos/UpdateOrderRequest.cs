namespace OrderService.Dtos;

public record UpdateOrderRequest(
    string CustomerName,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    string Status
);
