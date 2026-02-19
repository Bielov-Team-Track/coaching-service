using Shared.Enums;

namespace Shared.Messaging.Contracts.Events.Family;

public record ConsentRevokedEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public required Guid GuardianId { get; init; }
    public required Guid MinorId { get; init; }
    public required ConsentType ConsentType { get; init; }
}
