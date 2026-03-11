namespace Coaching.Application.Interfaces.Services;

/// <summary>
/// Event context returned from events-service for authorization decisions.
/// </summary>
public record EventContext(
    string EventType,
    string ContextType,
    Guid? ContextId);

/// <summary>
/// gRPC client for authorization checks against events-service.
/// </summary>
public interface IEventsGrpcClient
{
    /// <summary>
    /// Check if a user is an admin (organizer/co-organizer) of an event.
    /// </summary>
    Task<bool> IsEventAdminAsync(Guid eventId, Guid userId);

    /// <summary>
    /// Check if a user is a participant of an event and whether the event exists.
    /// </summary>
    Task<(bool IsParticipant, bool EventExists)> IsEventParticipantAsync(Guid eventId, Guid userId);

    /// <summary>
    /// Get the context of an event (type, context type, context ID) for authorization.
    /// Returns null if the event does not exist.
    /// </summary>
    Task<EventContext?> GetEventContextAsync(Guid eventId);
}
