using Shared.Models;

namespace Coaching.Domain.Models.Templates;

/// <summary>
/// A coach working an event's training plan. The plan's lead coach distributes the others
/// across the practice; this row is the whole-plan end of that, for a coach who is not tied
/// to one station.
/// </summary>
public class PlanCoach : BaseEntity
{
    public Guid PlanId { get; set; }

    /// <summary>
    /// Cross-service ref to the user, no FK. A coach is assigned from the event's participant
    /// roster, which can name someone whose profile replica has not arrived yet; an FK would
    /// turn that lag into a failed assignment.
    /// </summary>
    public Guid UserId { get; set; }

    // Navigation properties
    public virtual TrainingPlan Plan { get; set; } = null!;
}
