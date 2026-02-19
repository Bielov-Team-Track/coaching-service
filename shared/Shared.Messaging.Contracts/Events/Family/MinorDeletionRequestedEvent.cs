namespace Shared.Messaging.Contracts.Events.Family;

public record MinorDeletionRequestedEvent : INotificationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public required Guid UserId { get; init; }
    public required Guid MinorId { get; init; }
    public required Guid HouseholdId { get; init; }
    public required DateTime DeletionEffectiveAt { get; init; }
}
