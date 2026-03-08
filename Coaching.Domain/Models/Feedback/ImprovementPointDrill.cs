using Coaching.Domain.Models.Drills;
using Shared.Models;

namespace Coaching.Domain.Models.Feedback;

public class ImprovementPointDrill : BaseEntity
{
    public Guid ImprovementPointId { get; set; }
    public Guid DrillId { get; set; }

    public virtual ImprovementPoint ImprovementPoint { get; set; } = null!;
    public virtual Drill Drill { get; set; } = null!;  // Local FK - both in coaching-service
}
