using System.Collections.Concurrent;
using Modular.Common.Events;

namespace Modular.Orders.IntegrationTests;

internal sealed class FakeIntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly ConcurrentBag<object> _published = new();

    public Task PublishAsync(object message, CancellationToken cancellationToken = default)
    {
        _published.Add(message);
        return Task.CompletedTask;
    }

    public IEnumerable<T> Published<T>() => _published.OfType<T>();
}
