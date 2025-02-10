namespace Modular.Common;

public sealed class OutboxMessage
{
    public Guid Id { get; init; }
    public string Type { get; init; }
    public string Content { get; init; }
    public DateTime OccurredOnUtc { get; init; }
    public DateTime? ProcessedOnUtc { get; set; }
    public string? Error { get; set; }
}
