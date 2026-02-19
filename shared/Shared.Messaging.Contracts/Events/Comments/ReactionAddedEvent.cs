namespace Shared.Messaging.Contracts.Events.Comments;

public record ReactionAddedEvent : INotificationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public required Guid UserId { get; init; }  // Comment author receiving notification
    public required Guid TargetEventId { get; init; }
    public required string EventName { get; init; }
    public required Guid CommentId { get; init; }
    public required Guid ReactorId { get; init; }
    public required string ReactorName { get; init; }
    public required string Emoji { get; init; }
}
