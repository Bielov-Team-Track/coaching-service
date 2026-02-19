namespace Shared.Messaging.Contracts.Events.Family;

public record MinorTransitionedToAdultEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public required Guid UserId { get; init; }
    public required Guid HouseholdId { get; init; }
}
