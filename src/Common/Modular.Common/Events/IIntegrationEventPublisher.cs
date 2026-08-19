namespace Modular.Common.Events;

public interface IIntegrationEventPublisher
{
    Task PublishAsync(object message, CancellationToken cancellationToken = default);
}
