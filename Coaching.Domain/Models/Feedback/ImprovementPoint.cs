using Shared.Models;

namespace Coaching.Domain.Models.Feedback;

public class ImprovementPoint : BaseEntity
{
    public Guid FeedbackId { get; set; }
    public required string Description { get; set; }
    public int Order { get; set; }

    public virtual Feedback Feedback { get; set; } = null!;
    public virtual ICollection<ImprovementPointDrill> AttachedDrills { get; set; } = new List<ImprovementPointDrill>();
    public virtual ICollection<ImprovementPointMedia> MediaLinks { get; set; } = new List<ImprovementPointMedia>();
}
