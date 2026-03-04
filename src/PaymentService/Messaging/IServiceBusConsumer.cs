namespace PaymentService.Messaging;

public interface IServiceBusConsumer
{
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}
