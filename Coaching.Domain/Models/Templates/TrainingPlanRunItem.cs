using Shared.Models;

namespace Coaching.Domain.Models.Templates;

public class TrainingPlanRunItem : BaseEntity
{
    public Guid RunId { get; set; }
    public Guid PlanItemId { get; set; }

    // Snapshot so a deleted plan item still resolves a drill id. Null for the kinds
    // that never had one — a break is not a drill.
    public Guid? DrillId { get; set; }

    // Snapshot of item order at run start.
    public int Order { get; set; }

    // Snapshot of item.Duration * 60 at run start.
    public int PlannedDurationSeconds { get; set; }

    // Filled when leaving the item.
    public int ActualElapsedSeconds { get; set; }

    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    public virtual TrainingPlanRun Run { get; set; } = null!;
}
