using Coaching.Domain.Enums;
using Coaching.Domain.Models.Drills;
using Shared.Models;

namespace Coaching.Domain.Models.Templates;

/// <summary>
/// A row inside a station group. The same shape as a <see cref="PlanItem"/> but a separate
/// table on purpose: a group's rows are not the plan's rows. Sharing one table would put
/// them in every query that reads a plan's items — the spine, the ordering, the totals —
/// where they would be counted twice and drawn in the wrong place, and one forgotten filter
/// is all it would take.
/// </summary>
public class PlanStationItem : BaseEntity
{
    public Guid StationId { get; set; }

    public ItemKind Kind { get; set; } = ItemKind.Drill;

    /// <summary>Set only when <see cref="Kind"/> is <see cref="ItemKind.Drill"/>.</summary>
    public Guid? DrillId { get; set; }

    /// <summary>The row's own name, for the kinds that have no drill to borrow one from.</summary>
    public string? Title { get; set; }

    public int Order { get; set; }
    public int Duration { get; set; }
    public string? Notes { get; set; }

    // Navigation properties
    public virtual PlanStation Station { get; set; } = null!;
    public virtual Drill? Drill { get; set; }
}
