
namespace Shared.Messaging.Contracts.Events.Events;

public class EventInvitationCreatedEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public Guid InvitedUserId { get; set; }
    public Guid InvitationEventId { get; set; }

    public string EventName { get; set; }
    public string EventLocationName { get; set; }
    public DateTime EventDateTime { get; set; }
}
