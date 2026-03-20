namespace OrderService.Messaging;

public class ServiceBusOptions
{
    public string FullyQualifiedNamespace { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
    public string QueueName { get; set; } = "order-created";
    public string TopicName { get; set; } = "order-created-topic";
    public bool Enabled { get; set; } = false;
    public bool UseManagedIdentity { get; set; } = true;
}
