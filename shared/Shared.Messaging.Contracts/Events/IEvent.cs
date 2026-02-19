namespace Shared.Messaging.Contracts.Events;

public interface IEvent
{
    Guid EventId { get; }
    DateTime Timestamp { get; }
}