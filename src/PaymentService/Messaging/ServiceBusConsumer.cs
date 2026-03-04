using Azure.Messaging.ServiceBus;
using System.Text.Json;
using PaymentService.Events;
using PaymentService.Services;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

namespace PaymentService.Messaging;

public class ServiceBusConsumer : BackgroundService, IServiceBusConsumer
{
    private readonly ServiceBusOptions _options;
    private readonly ILogger<ServiceBusConsumer> _logger;
    private readonly IServiceProvider _serviceProvider;
    private ServiceBusClient? _client;
    private ServiceBusProcessor? _processor;

    public ServiceBusConsumer(
        IOptions<ServiceBusOptions> options,
        ILogger<ServiceBusConsumer> logger,
        IServiceProvider serviceProvider)
    {
        _options = options.Value;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Service Bus consumer is disabled in configuration");
            return;
        }

        try
        {
            InitializeProcessor();

            // Register handlers
            _processor!.ProcessMessageAsync += ProcessMessageAsync;
            _processor.ProcessErrorAsync += ProcessErrorAsync;

            await _processor.StartProcessingAsync(stoppingToken);
            _logger.LogInformation("Service Bus consumer started successfully");

            // Keep the service running
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Service Bus consumer cancellation requested");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in Service Bus consumer");
            throw;
        }
    }

    private void InitializeProcessor()
    {
        try
        {
            if (!string.IsNullOrEmpty(_options.FullyQualifiedNamespace))
            {
                _client = new ServiceBusClient(_options.FullyQualifiedNamespace, new Azure.Identity.DefaultAzureCredential());
            }
            else if (!string.IsNullOrEmpty(_options.ConnectionString))
            {
                _client = new ServiceBusClient(_options.ConnectionString);
            }
            else
            {
                throw new InvalidOperationException("Service Bus credentials not configured");
            }

            var options = new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = false,
                MaxConcurrentCalls = _options.MaxConcurrentCalls
            };

            _processor = _client.CreateProcessor(_options.QueueName, options);
            _logger.LogInformation($"Service Bus processor initialized for queue: {_options.QueueName}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Service Bus processor");
            throw;
        }
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        try
        {
            var messageBody = args.Message.Body.ToString();
            _logger.LogInformation($"Processing message: {args.Message.CorrelationId}");

            var orderEvent = JsonSerializer.Deserialize<OrderCreatedEvent>(messageBody);
            if (orderEvent is null)
            {
                _logger.LogError("Failed to deserialize OrderCreatedEvent");
                await args.DeadLetterMessageAsync(args.Message, "InvalidMessageFormat", "Failed to deserialize message");
                return;
            }

            // Create payment for the order
            var payment = new PaymentService.Models.Payment
            {
                TransactionId = Guid.NewGuid(),
                OrderId = orderEvent.OrderId,
                Amount = orderEvent.TotalAmount,
                Currency = "USD",
                Method = "Pending",
                Status = "Pending",
                CreatedAtUtc = DateTime.UtcNow
            };

            using var scope = _serviceProvider.CreateScope();
            var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentService>();

            await paymentService.CreatePaymentAsync(payment);
            _logger.LogInformation($"Payment created for order {orderEvent.OrderId}");

            // Complete the message
            await args.CompleteMessageAsync(args.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing message {args.Message.CorrelationId}");
            
            // Let message be retried (don't complete)
            // After MaxDeliveryCount retries, it will go to DLQ
        }
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(
            args.Exception,
            $"Message handler encountered an exception. Endpoint: {args.FullyQualifiedNamespace}, Entity Path: {args.EntityPath}");
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_processor is not null)
        {
            await _processor.StopProcessingAsync(cancellationToken);
            await _processor.DisposeAsync();
        }

        if (_client is not null)
        {
            await _client.DisposeAsync();
        }

        await base.StopAsync(cancellationToken);
    }

    async Task IServiceBusConsumer.StartAsync(CancellationToken cancellationToken)
    {
        await base.StartAsync(cancellationToken);
    }

    async Task IServiceBusConsumer.StopAsync(CancellationToken cancellationToken)
    {
        await StopAsync(cancellationToken);
    }
}
