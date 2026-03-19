namespace NotificationFunction.Notifications;

public interface INotificationSender
{
    Task SendOrderCreatedAsync(OrderCreatedEvent orderCreatedEvent, string messageId, CancellationToken cancellationToken);
}