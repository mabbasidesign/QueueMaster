namespace NotificationFunction.Notifications;

public class NotificationEmailOptions
{
    public const string SectionName = "Notification";

    public string ConnectionString { get; set; } = string.Empty;

    public string SenderAddress { get; set; } = string.Empty;

    public string RecipientAddress { get; set; } = string.Empty;
}