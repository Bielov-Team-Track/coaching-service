namespace Shared.Messaging.Contracts.Events;

public interface INotificationEvent : IEvent
{
    Guid UserId { get; }
}