using Azure.Messaging.ServiceBus;
using System.Text.Json;
using OrderService.Events;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace OrderService.Messaging;

public class ServiceBusPublisher : IServiceBusPublisher
{
    private readonly ServiceBusOptions _options;
    private readonly ILogger<ServiceBusPublisher> _logger;
    private ServiceBusClient? _client;
    private ServiceBusSender? _sender;

    public ServiceBusPublisher(IOptions<ServiceBusOptions> options, ILogger<ServiceBusPublisher> logger)
    {
        _options = options.Value;
        _logger = logger;
        
        if (_options.Enabled)
        {
            InitializeClient();
        }
    }

    private void InitializeClient()
    {
        try
        {
            if (_options.UseManagedIdentity && !string.IsNullOrEmpty(_options.FullyQualifiedNamespace))
            {
                _client = new ServiceBusClient(_options.FullyQualifiedNamespace, new Azure.Identity.DefaultAzureCredential());
            }
            else if (!string.IsNullOrEmpty(_options.ConnectionString))
            {
                _client = new ServiceBusClient(_options.ConnectionString);
            }
            else
            {
                _logger.LogWarning("Service Bus is enabled but no credentials were configured");
                return;
            }

            // _sender = _client.CreateSender(_options.QueueName);
            _sender = _client.CreateSender(_options.TopicName);
            _logger.LogInformation($"Service Bus client initialized for topic: {_options.TopicName}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Service Bus client");
            throw;
        }
    }

    public async Task PublishOrderCreatedAsync(int orderId, string customerName, string productName, int quantity, decimal unitPrice, decimal totalAmount, DateTime createdAtUtc)
    {
        if (!_options.Enabled || _sender is null)
        {
            _logger.LogInformation($"Service Bus disabled, skipping publish for order {orderId}");
            return;
        }

        try
        {
            var orderEvent = new OrderCreatedEvent(
                orderId,
                customerName,
                productName,
                quantity,
                unitPrice,
                totalAmount,
                createdAtUtc
            );

            var message = new ServiceBusMessage(JsonSerializer.Serialize(orderEvent))
            {
                ContentType = "application/json",
                Subject = "OrderCreated",
                CorrelationId = $"order-{orderId}-{DateTime.UtcNow.Ticks}"
            };

            await _sender.SendMessageAsync(message);
            _logger.LogInformation($"Published OrderCreated event for order {orderId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to publish OrderCreated event for order {orderId}");
            throw;
        }
    }

    public async Task<bool> PublishAsync(string eventType, string jsonPayload, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || _sender is null)
        {
            _logger.LogInformation($"Service Bus disabled, skipping publish for event type {eventType}");
            return false;
        }

        try
        {
            var message = new ServiceBusMessage(jsonPayload)
            {
                ContentType = "application/json",
                Subject = eventType,
                CorrelationId = $"{eventType}-{DateTime.UtcNow.Ticks}"
            };

            await _sender.SendMessageAsync(message, cancellationToken);
            _logger.LogInformation($"Published {eventType} event to Service Bus");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to publish {eventType} event to Service Bus");
            throw;
        }
    }

    public void Dispose()
    {
        _sender?.DisposeAsync().GetAwaiter().GetResult();
        _client?.DisposeAsync().GetAwaiter().GetResult();
    }
}
