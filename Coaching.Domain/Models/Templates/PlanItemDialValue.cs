using Shared.Models;

namespace Coaching.Domain.Models.Templates;

/// <summary>
/// What one use of a drill decided a dial should say. Exactly one of <see cref="ItemId"/> and
/// <see cref="StationItemId"/> is set, because a use is either a row on the plan's spine or a
/// row inside a station group.
///
/// Neither carries a foreign key on purpose: a use is identified by an id that spans two tables,
/// which one column cannot point at. They belong to the plan — that is the FK — and a save that
/// drops a use deletes its answers explicitly rather than leaning on a cascade.
/// </summary>
public class PlanItemDialValue : BaseEntity
{
    public const int DialNameMaxLength = 60;
    public const int ValueMaxLength = 500;

    public Guid PlanId { get; set; }

    /// <summary>A TemplateItems id. Null when the use is inside a station group.</summary>
    public Guid? ItemId { get; set; }

    /// <summary>A PlanStationItems id. Null when the use is a row on the plan's spine.</summary>
    public Guid? StationItemId { get; set; }

    public required string DialName { get; set; }

    public string Value { get; set; } = string.Empty;

    // Navigation properties
    public virtual TrainingPlan? Plan { get; set; }
}
