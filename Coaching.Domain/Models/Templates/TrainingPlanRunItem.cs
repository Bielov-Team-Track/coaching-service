using Coaching.Domain.Enums;
using Shared.Models;

namespace Coaching.Domain.Models.Templates;

public class TrainingPlanRunItem : BaseEntity
{
    public Guid RunId { get; set; }
    public Guid PlanItemId { get; set; }

    // Snapshot of what the row is, on the same principle as DrillId below: a run that has to
    // reach back into the plan to learn a row is a break cannot describe itself once the plan
    // has moved on.
    public ItemKind Kind { get; set; } = ItemKind.Drill;

    // The row's own name, for the kinds that have no drill to borrow one from.
    public string? Title { get; set; }

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

    /// <summary>The groups running side by side. Only a Stations row has any.</summary>
    public virtual ICollection<RunStation> Stations { get; set; } = new List<RunStation>();
}
