using Azure.Messaging.ServiceBus;
using System.Text.Json;
using PaymentService.Events;
using PaymentService.Services;
using PaymentService.Data;
using PaymentService.Models;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

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
                throw new InvalidOperationException("Service Bus credentials not configured");
            }

            var options = new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = false,
                MaxConcurrentCalls = _options.MaxConcurrentCalls
            };

            // _processor = _client.CreateProcessor(_options.QueueName, options);
            _processor = _client.CreateProcessor(_options.TopicName, _options.SubscriptionName, options);
            _logger.LogInformation(
                "Service Bus processor initialized for topic: {TopicName}, subscription: {SubscriptionName}",
                _options.TopicName,
                _options.SubscriptionName);
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
            _logger.LogInformation("Processing message with CorrelationId={CorrelationId}", args.Message.CorrelationId);

            var orderEvent = JsonSerializer.Deserialize<OrderCreatedEvent>(messageBody);
            if (orderEvent is null)
            {
                _logger.LogError("Failed to deserialize OrderCreatedEvent");
                // Bad message format -> send directly to DLQ (no retry).
                await args.DeadLetterMessageAsync(args.Message, "InvalidMessageFormat", "Failed to deserialize message");
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();

            var idempotencyKey = GetIdempotencyKey(args.Message.MessageId, orderEvent.OrderId);

            var alreadyProcessed = await IsAlreadyProcessedAsync(db, idempotencyKey);

            if (alreadyProcessed)
            {
                _logger.LogWarning(
                    "Duplicate message detected. MessageId={MessageId}, OrderId={OrderId}. Skipping.",
                    idempotencyKey,
                    orderEvent.OrderId);
                await args.CompleteMessageAsync(args.Message);
                return;
            }

            var payment = CreatePayment(orderEvent);

            db.Payments.Add(payment);
            db.ProcessedMessages.Add(new ProcessedMessage
            {
                MessageId = idempotencyKey,
                OrderId = orderEvent.OrderId,
                ProcessedAtUtc = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
            _logger.LogInformation("Payment created for OrderId={OrderId}", orderEvent.OrderId);

            // Success -> complete message so there is no retry.
            await args.CompleteMessageAsync(args.Message);
        }
        catch (DbUpdateException ex) when (IsDuplicateMessageIdViolation(ex))
        {
            // Another concurrent handler already processed this same message.
            _logger.LogWarning(
                "Duplicate message detected by unique key. MessageId={MessageId}. Skipping.",
                args.Message.MessageId);
            await args.CompleteMessageAsync(args.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message with CorrelationId={CorrelationId}", args.Message.CorrelationId);
            
            // Processing failed -> do not complete, Service Bus will retry.
            // Too many retries -> Service Bus moves it to DLQ.
        }
    }

    private static string GetIdempotencyKey(string? messageId, int orderId)
    {
        return string.IsNullOrWhiteSpace(messageId)
            ? $"order-{orderId}"
            : messageId;
    }

    private static Task<bool> IsAlreadyProcessedAsync(PaymentDbContext db, string idempotencyKey)
    {
        return db.ProcessedMessages
            .AsNoTracking()
            .AnyAsync(m => m.MessageId == idempotencyKey);
    }

    private static Payment CreatePayment(OrderCreatedEvent orderEvent)
    {
        return new Payment
        {
            TransactionId = Guid.NewGuid(),
            OrderId = orderEvent.OrderId,
            Amount = orderEvent.TotalAmount,
            Currency = "USD",
            Method = "Auto",
            Status = "Processing",
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    private static bool IsDuplicateMessageIdViolation(DbUpdateException ex)
    {
        // SQL Server duplicate key errors: 2627 (unique constraint), 2601 (unique index)
        return ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2627 || sqlEx.Number == 2601);
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
