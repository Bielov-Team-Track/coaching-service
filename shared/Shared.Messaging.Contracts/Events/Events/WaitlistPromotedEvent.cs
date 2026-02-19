namespace Shared.Messaging.Contracts.Events.Events;

public record WaitlistPromotedEvent : INotificationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public required Guid UserId { get; init; }  // Promoted user receiving notification
    public required Guid TargetEventId { get; init; }
    public required string EventName { get; init; }
    public required string TeamName { get; init; }
    public required string PositionName { get; init; }
}
