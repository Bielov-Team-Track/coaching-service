namespace Shared.Messaging.Contracts.Events.Family;

public record MinorAccountCreatedEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public required Guid MinorUserId { get; init; }
    public required Guid HouseholdId { get; init; }
    public required Guid CreatedByGuardianId { get; init; }
    public required string Name { get; init; }
    public required DateTime DateOfBirth { get; init; }
    public required string CountryCode { get; init; }
}
