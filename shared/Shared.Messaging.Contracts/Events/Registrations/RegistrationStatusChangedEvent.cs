namespace Shared.Messaging.Contracts.Events.Registrations;

public record RegistrationStatusChangedEvent : INotificationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public required Guid UserId { get; init; }
    public required Guid RegistrationId { get; init; }
    public required Guid ClubId { get; init; }
    public required string ClubName { get; init; }
    public string? ClubLogoUrl { get; set; }
    public required string OldStatus { get; init; }
    public required string NewStatus { get; init; }
    public string? PublicNote { get; init; }
}