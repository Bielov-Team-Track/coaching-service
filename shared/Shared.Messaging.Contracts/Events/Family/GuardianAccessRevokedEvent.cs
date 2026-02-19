namespace Shared.Messaging.Contracts.Events.Family;

public record GuardianAccessRevokedEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public required int Version { get; init; }
    public required Guid HouseholdId { get; init; }
    public required Guid GuardianId { get; init; }
    public required Guid MinorId { get; init; }
    public required DateTime RevokedAt { get; init; }
}
