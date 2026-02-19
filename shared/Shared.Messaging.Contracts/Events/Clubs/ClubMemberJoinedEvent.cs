namespace Shared.Messaging.Contracts.Events.Clubs;

public record ClubMemberJoinedEvent : INotificationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public required Guid UserId { get; init; }
    public required Guid ClubId { get; init; }
    public required string ClubName { get; init; }
    public required Guid MemberId { get; init; }
    public required string MemberName { get; init; }
}