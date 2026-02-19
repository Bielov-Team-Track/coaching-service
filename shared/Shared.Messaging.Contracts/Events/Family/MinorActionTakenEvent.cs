using Shared.Enums;

namespace Shared.Messaging.Contracts.Events.Family;

public record MinorActionTakenEvent : INotificationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public required Guid UserId { get; init; }
    public required Guid MinorId { get; init; }
    public required string MinorName { get; init; }
    public required string ActionDescription { get; init; }
    public required ActionRiskLevel RiskLevel { get; init; }
    public required string ActionType { get; init; }
    public Guid? RelatedEntityId { get; init; }
}
