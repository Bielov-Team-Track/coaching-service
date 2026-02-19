namespace Shared.Messaging.Contracts.Events.Events;

public record PaymentReceivedEvent : INotificationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public required Guid UserId { get; init; }  // Organizer receiving notification
    public required Guid TargetEventId { get; init; }
    public required string EventName { get; init; }
    public required Guid PaidUserId { get; init; }
    public required string PaidUserName { get; init; }
    public required decimal Amount { get; init; }
    public required string Currency { get; init; }
}
