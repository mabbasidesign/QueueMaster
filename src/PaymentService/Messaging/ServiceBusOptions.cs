namespace PaymentService.Messaging;

public class ServiceBusOptions
{
    public string FullyQualifiedNamespace { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
    public string QueueName { get; set; } = "order-created";
    public bool Enabled { get; set; } = false;
    public bool UseManagedIdentity { get; set; } = true;
    public int MaxDeliveryCount { get; set; } = 5;
    public int MaxConcurrentCalls { get; set; } = 4;
}
