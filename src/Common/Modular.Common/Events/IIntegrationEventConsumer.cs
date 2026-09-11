namespace Modular.Common.Events;

public interface IIntegrationEventConsumer<in TMessage> where TMessage : notnull
{
    Task ConsumeAsync(TMessage message, CancellationToken cancellationToken);
}
