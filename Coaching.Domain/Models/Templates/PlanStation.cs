using Shared.Models;

namespace Coaching.Domain.Models.Templates;

/// <summary>
/// One group inside a Stations block: a set of players running their own drills while the
/// other groups run theirs. The block's rows are the groups; the groups hold the drills.
/// </summary>
public class PlanStation : BaseEntity
{
    public const int NameMaxLength = 100;

    /// <summary>The Stations row this group belongs to.</summary>
    public Guid PlanItemId { get; set; }

    public required string Name { get; set; }
    public int Order { get; set; }

    // Navigation properties
    public virtual PlanItem Item { get; set; } = null!;
    public virtual ICollection<PlanStationItem> Items { get; set; } = new List<PlanStationItem>();
}
