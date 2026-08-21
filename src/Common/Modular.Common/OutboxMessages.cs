namespace Modular.Common;

public sealed class OutboxMessage
{
    public Guid Id { get; init; }
    public string Type { get; init; }
    public string Content { get; init; }
    public DateTime OccurredOnUtc { get; init; }
    public DateTime? ProcessedOnUtc { get; set; }
    public string? Error { get; set; }

    // W3C traceparent of the request that raised the domain event (Activity.Current?.Id at write time).
    // Replayed as an ActivityLink when the outbox job publishes, so the trace can be followed from the
    // originating request through to whatever eventually consumes the resulting integration event.
    public string? TraceParent { get; init; }
}
