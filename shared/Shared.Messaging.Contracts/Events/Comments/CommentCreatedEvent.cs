namespace Shared.Messaging.Contracts.Events.Comments;

public record CommentCreatedEvent : INotificationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public required Guid UserId { get; init; }  // Organizer receiving notification
    public required Guid TargetEventId { get; init; }
    public required string EventName { get; init; }
    public required Guid CommentId { get; init; }
    public required Guid CommentAuthorId { get; init; }
    public required string CommentAuthorName { get; init; }
    public required string CommentPreview { get; init; }
}
