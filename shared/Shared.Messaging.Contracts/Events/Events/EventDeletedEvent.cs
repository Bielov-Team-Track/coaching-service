namespace Shared.Messaging.Contracts.Events.Events;

public record EventDeletedEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public required Guid TargetEventId { get; init; }
}
