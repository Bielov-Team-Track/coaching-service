namespace Shared.Messaging.Contracts.Events.Events;

public record InvitationAcceptedEvent : INotificationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public required Guid UserId { get; init; }  // Organizer receiving notification
    public required Guid TargetEventId { get; init; }
    public required string EventName { get; init; }
    public required Guid AcceptedUserId { get; init; }
    public required string AcceptedUserName { get; init; }
}
