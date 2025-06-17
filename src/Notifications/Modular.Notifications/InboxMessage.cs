namespace Modular.Notifications;
public sealed class InboxMessage
{
    public Guid Id { get; set; }
    public string MessageType { get; set; }
    public string Payload { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
}

