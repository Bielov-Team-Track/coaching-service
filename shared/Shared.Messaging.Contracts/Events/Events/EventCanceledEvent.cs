namespace Shared.Messaging.Contracts.Events.Events;

public record EventCanceledEvent : INotificationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public required Guid UserId { get; init; }  // Participant receiving notification
    public required Guid TargetEventId { get; init; }
    public required string EventName { get; init; }
    public required string CanceledByUserName { get; init; }
    public string? CancellationReason { get; init; }
}
