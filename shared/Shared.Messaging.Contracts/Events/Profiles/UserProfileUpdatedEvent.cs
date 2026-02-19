using Shared.Models;

namespace Shared.Messaging.Contracts.Events.Profiles;

public record UserProfileUpdatedEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public required UserProfile UserProfile { get; set; }
}
