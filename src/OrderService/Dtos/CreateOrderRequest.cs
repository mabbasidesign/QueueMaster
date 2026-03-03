namespace OrderService.Dtos;

public record CreateOrderRequest(
    string CustomerName,
    string ProductName,
    int Quantity,
    decimal UnitPrice
);
