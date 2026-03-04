namespace OrderService.Messaging;

public interface IServiceBusPublisher
{
    Task PublishOrderCreatedAsync(int orderId, string customerName, string productName, int quantity, decimal unitPrice, decimal totalAmount, DateTime createdAtUtc);
    Task PublishAsync(string eventType, string jsonPayload, CancellationToken cancellationToken = default);
}
