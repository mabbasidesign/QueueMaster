using Microsoft.EntityFrameworkCore;
using OrderService.Data;

namespace OrderService.Messaging;

public class OutboxPublisher : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxPublisher> _logger;
    // Retry interval for unpublished events.
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(30);

    public OutboxPublisher(IServiceProvider serviceProvider, ILogger<OutboxPublisher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxPublisher service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxEventsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox events");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }

        _logger.LogInformation("OutboxPublisher service stopped");
    }

    private async Task ProcessOutboxEventsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IServiceBusPublisher>();

        // Get unpublished events
        var unpublishedEvents = await context.OutboxEvents
            .Where(e => !e.IsPublished)
            .OrderBy(e => e.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        if (unpublishedEvents.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Processing {Count} unpublished outbox events", unpublishedEvents.Count);

        foreach (var outboxEvent in unpublishedEvents)
        {
            try
            {
                // Try to publish the event to Service Bus.
                var published = await publisher.PublishAsync(outboxEvent.EventType, outboxEvent.Payload, cancellationToken);

                if (!published)
                {
                    // Publish was skipped (for example Service Bus disabled).
                    // Keep event as unpublished so it will be retried on next polling cycle.
                    _logger.LogInformation(
                        "Service Bus publish skipped for outbox event {EventId}; event remains unpublished",
                        outboxEvent.Id);
                    continue;
                }

                // Publish succeeded -> mark published so it is not retried again.
                outboxEvent.IsPublished = true;
                outboxEvent.PublishedAt = DateTime.UtcNow;
                
                await context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully published outbox event {EventId} of type {EventType}", 
                    outboxEvent.Id, outboxEvent.EventType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish outbox event {EventId}", outboxEvent.Id);

                // Publish failed -> increment retry count and keep event unpublished.
                // This means it will be retried in the next loop.
                outboxEvent.RetryCount++;
                outboxEvent.LastError = ex.Message;
                
                await context.SaveChangesAsync(cancellationToken);

                // No DLQ here because this is producer/outbox side, not consumer side.
                // After 5+ failures we currently only log warning and keep retrying.
                if (outboxEvent.RetryCount >= 5)
                {
                    _logger.LogWarning("Outbox event {EventId} has exceeded max retry count", outboxEvent.Id);
                }
            }
        }
    }
}
