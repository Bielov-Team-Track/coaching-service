namespace Shared.Messaging.Contracts.Events.Events;

public record UserBannedFromEventEvent : INotificationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public required Guid UserId { get; init; }  // Banned user receiving notification
    public required Guid TargetEventId { get; init; }
    public required string EventName { get; init; }
    public required string BannedByUserName { get; init; }
    public string? Reason { get; init; }
}
