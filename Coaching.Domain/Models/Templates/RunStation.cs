using Shared.Models;

namespace Coaching.Domain.Models.Templates;

/// <summary>
/// A group of a Stations row, snapshotted at run start. The plan's own groups can be edited
/// mid-session — or the block deleted outright — and a coach halfway through the practice must
/// still see what they set out to run, so the run keeps its own copy exactly as
/// <see cref="TrainingPlanRunItem"/> keeps its own drill id.
/// </summary>
public class RunStation : BaseEntity
{
    public Guid RunItemId { get; set; }

    public required string Name { get; set; }
    public int Order { get; set; }

    public virtual TrainingPlanRunItem RunItem { get; set; } = null!;
    public virtual ICollection<RunStationItem> Items { get; set; } = new List<RunStationItem>();
}
