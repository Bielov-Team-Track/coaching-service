using Shared.Models;

namespace Coaching.Domain.Models.Drills;

public class DrillLike : BaseEntity
{
    public Guid DrillId { get; set; }
    public Guid UserId { get; set; }

    // Navigation properties
    public virtual Drill Drill { get; set; } = null!;
    public virtual UserProfile? User { get; set; }
}
