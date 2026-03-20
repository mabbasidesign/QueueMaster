namespace NotificationFunction.Notifications;

public class NotificationEmailOptions
{
    public const string SectionName = "Notification";

    public string ConnectionString { get; set; } = string.Empty;

    public string SenderAddress { get; set; } = string.Empty;

    // Comma-separated email list, e.g. "a@contoso.com,b@contoso.com".
    public string RecipientAddresses { get; set; } = string.Empty;

    // Backward compatibility for legacy single-recipient setting.
    public string RecipientAddress { get; set; } = string.Empty;
}