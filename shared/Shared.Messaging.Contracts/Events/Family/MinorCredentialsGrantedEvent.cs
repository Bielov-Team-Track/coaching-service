namespace Shared.Messaging.Contracts.Events.Family;

public record MinorCredentialsGrantedEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public required Guid MinorUserId { get; init; }
    public required string Email { get; init; }
}
