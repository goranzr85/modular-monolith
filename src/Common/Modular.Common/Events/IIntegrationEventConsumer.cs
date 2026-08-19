namespace Modular.Common.Events;

public interface IIntegrationEventConsumer<TMessage> where TMessage : notnull
{
    Task ConsumeAsync(TMessage message, CancellationToken cancellationToken);
}
