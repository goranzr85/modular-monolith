namespace Modular.Common.Events;

public interface IIntegrationEventPublisher
{
    // causationTraceParent: the W3C traceparent of whatever originally caused this message (e.g. the outbox
    // row's OutboxMessage.TraceParent) - recorded as an ActivityLink on the publish span rather than as its
    // parent, since the delay between cause and publish is unbounded and a fanout exchange can have several
    // independent consumers.
    Task PublishAsync(object message, string? causationTraceParent = null, CancellationToken cancellationToken = default);
}
