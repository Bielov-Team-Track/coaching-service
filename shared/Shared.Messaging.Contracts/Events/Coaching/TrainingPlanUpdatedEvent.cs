namespace Shared.Messaging.Contracts.Events.Coaching;

public record TrainingPlanUpdatedEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public required Guid PlanId { get; init; }
    public required Guid? TargetEventId { get; init; }
    public required string Action { get; init; } // "Created", "Updated", "Deleted"

    // Denormalized summary for events-service to cache
    public string? PlanName { get; init; }
    public int TotalDuration { get; init; }
    public int SectionCount { get; init; }
    public int DrillCount { get; init; }
}
