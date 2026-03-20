using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NotificationFunction.Notifications;

public class AcsEmailNotificationSender : INotificationSender
{
    private readonly ILogger<AcsEmailNotificationSender> _logger;
    private readonly NotificationEmailOptions _options;

    public AcsEmailNotificationSender(
        ILogger<AcsEmailNotificationSender> logger,
        IOptions<NotificationEmailOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public async Task SendOrderCreatedAsync(OrderCreatedEvent orderCreatedEvent, string messageId, CancellationToken cancellationToken)
    {
        var recipients = ResolveRecipients();

        if (string.IsNullOrWhiteSpace(_options.ConnectionString) ||
            string.IsNullOrWhiteSpace(_options.SenderAddress) ||
            recipients.Count == 0)
        {
            throw new InvalidOperationException("Notification email settings are incomplete. Configure Notification:ConnectionString, Notification:SenderAddress, and Notification:RecipientAddresses (or Notification:RecipientAddress for legacy single recipient).");
        }

        var client = new EmailClient(_options.ConnectionString);
        var subject = $"Order created: #{orderCreatedEvent.OrderId}";
        var plainTextContent = $"""
            A new order was created.

            Message ID: {messageId}
            Order ID: {orderCreatedEvent.OrderId}
            Customer: {orderCreatedEvent.CustomerName}
            Product: {orderCreatedEvent.ProductName}
            Quantity: {orderCreatedEvent.Quantity}
            Total: {orderCreatedEvent.TotalAmount:C}
            Created (UTC): {orderCreatedEvent.CreatedAtUtc:O}
            """;
        var htmlContent = $"""
            <h2>New order created</h2>
            <ul>
              <li><strong>Message ID:</strong> {messageId}</li>
              <li><strong>Order ID:</strong> {orderCreatedEvent.OrderId}</li>
              <li><strong>Customer:</strong> {orderCreatedEvent.CustomerName}</li>
              <li><strong>Product:</strong> {orderCreatedEvent.ProductName}</li>
              <li><strong>Quantity:</strong> {orderCreatedEvent.Quantity}</li>
              <li><strong>Total:</strong> {orderCreatedEvent.TotalAmount:C}</li>
              <li><strong>Created (UTC):</strong> {orderCreatedEvent.CreatedAtUtc:O}</li>
            </ul>
            """;

        var message = new EmailMessage(
            senderAddress: _options.SenderAddress,
            content: new EmailContent(subject)
            {
                PlainText = plainTextContent,
                Html = htmlContent
            },
            recipients: new EmailRecipients(recipients));

        var operation = await client.SendAsync(WaitUntil.Completed, message, cancellationToken);

        _logger.LogInformation(
            "Notification email sent for order {OrderId}. MessageId: {MessageId}, Status: {Status}",
            orderCreatedEvent.OrderId,
            messageId,
            operation.Value.Status);
    }

    private List<EmailAddress> ResolveRecipients()
    {
        var raw = string.IsNullOrWhiteSpace(_options.RecipientAddresses)
            ? _options.RecipientAddress
            : _options.RecipientAddresses;

        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        var entries = raw
            .Split([',', ';', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return entries.Select(address => new EmailAddress(address)).ToList();
    }
}