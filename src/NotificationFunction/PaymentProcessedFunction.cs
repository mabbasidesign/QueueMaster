using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NotificationFunction.Notifications;

namespace NotificationFunction;

public record OrderCreatedEvent(
    int OrderId,
    string CustomerName,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal TotalAmount,
    DateTime CreatedAtUtc);

public class OrderCreatedFunction
{
    private readonly ILogger<OrderCreatedFunction> _logger;
    private readonly INotificationSender _notificationSender;

    public OrderCreatedFunction(
        ILogger<OrderCreatedFunction> logger,
        INotificationSender notificationSender)
    {
        _logger = logger;
        _notificationSender = notificationSender;
    }

    [Function(nameof(OrderCreatedFunction))]
    public async Task Run(
        [ServiceBusTrigger("order-created", Connection = "ServiceBusConnection")]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
    {
        var body = message.Body.ToString();
        var evt = JsonSerializer.Deserialize<OrderCreatedEvent>(body);

        if (evt is null)
        {
            _logger.LogError("Failed to deserialize OrderCreatedEvent. MessageId: {Id}", message.MessageId);
            await messageActions.DeadLetterMessageAsync(
                message,
                deadLetterReason: "InvalidFormat",
                deadLetterErrorDescription: "Failed to deserialize OrderCreatedEvent");
            return;
        }

        _logger.LogInformation(
            "[EMAIL] Order created - Order: {OrderId}, Customer: {Customer}, Product: {Product}, Qty: {Quantity}, Total: {Total}",
            evt.OrderId, evt.CustomerName, evt.ProductName, evt.Quantity, evt.TotalAmount);

        await _notificationSender.SendOrderCreatedAsync(evt, message.MessageId, CancellationToken.None);
        await messageActions.CompleteMessageAsync(message);
    }
}