using Shared.Models;

namespace Coaching.Domain.Models.Templates;

/// <summary>
/// A coach working one station group. This is what the distribution is for: a coach opening
/// the plan sees the drills of the station they were given, not the whole practice.
/// </summary>
public class PlanStationCoach : BaseEntity
{
    public Guid StationId { get; set; }

    /// <summary>Cross-service ref to the user, no FK — see <see cref="PlanCoach.UserId"/>.</summary>
    public Guid UserId { get; set; }

    // Navigation properties
    public virtual PlanStation Station { get; set; } = null!;
}
