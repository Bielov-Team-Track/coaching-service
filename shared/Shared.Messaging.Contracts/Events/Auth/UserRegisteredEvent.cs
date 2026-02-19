namespace Shared.Messaging.Contracts.Events.Auth;

public class UserRegisteredEvent : IEvent
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;

    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
