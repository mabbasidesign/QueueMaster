using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

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

    public OrderCreatedFunction(ILogger<OrderCreatedFunction> logger)
    {
        _logger = logger;
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

        // TODO: plug in SendGrid or Azure Communication Services here
        await messageActions.CompleteMessageAsync(message);
    }
}