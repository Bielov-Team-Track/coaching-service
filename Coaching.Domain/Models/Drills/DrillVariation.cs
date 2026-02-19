using Shared.Models;

namespace Coaching.Domain.Models.Drills;

/// <summary>
/// Represents a variation relationship between two drills.
/// The SourceDrill has the TargetDrill as one of its variations.
/// </summary>
public class DrillVariation : BaseEntity
{
    public Guid SourceDrillId { get; set; }
    public Guid TargetDrillId { get; set; }

    /// <summary>
    /// Optional note explaining how this drill is a variation of the source
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Order of this variation in the source drill's variation list
    /// </summary>
    public int Order { get; set; }

    // Navigation properties
    public virtual Drill? SourceDrill { get; set; }
    public virtual Drill? TargetDrill { get; set; }
}
