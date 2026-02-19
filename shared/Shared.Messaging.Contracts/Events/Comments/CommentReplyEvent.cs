namespace Shared.Messaging.Contracts.Events.Comments;

public record CommentReplyEvent : INotificationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public required Guid UserId { get; init; }  // Original commenter receiving notification
    public required Guid TargetEventId { get; init; }
    public required string EventName { get; init; }
    public required Guid CommentId { get; init; }
    public required Guid ReplyAuthorId { get; init; }
    public required string ReplyAuthorName { get; init; }
    public required string ReplyPreview { get; init; }
}
