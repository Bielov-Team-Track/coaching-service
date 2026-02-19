using Shared.Models;

namespace Shared.Messaging.Contracts.Events.Events;

public record EventCreatedEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public required UserProfile UserProfile { get; set; }
}
