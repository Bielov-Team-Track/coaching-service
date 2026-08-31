using Coaching.Domain.Enums;
using Shared.Models;

namespace Coaching.Domain.Models.Templates;

/// <summary>
/// One row inside a snapshotted group. Lengths are seconds here, not minutes: a run counts
/// down, and the conversion belongs at the one place the snapshot is taken rather than at
/// every screen that reads it.
/// </summary>
public class RunStationItem : BaseEntity
{
    public Guid RunStationId { get; set; }

    public ItemKind Kind { get; set; } = ItemKind.Drill;

    // Snapshot so a deleted drill still resolves an id. Null for the kinds that never had one.
    public Guid? DrillId { get; set; }

    public string? Title { get; set; }

    public int Order { get; set; }
    public int DurationSeconds { get; set; }
    public string? Notes { get; set; }

    public virtual RunStation Station { get; set; } = null!;
}
