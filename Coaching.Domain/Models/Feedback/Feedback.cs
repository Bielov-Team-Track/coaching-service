using Shared.Models;

namespace Coaching.Domain.Models.Feedback;

public class Feedback : BaseEntity
{
    public Guid RecipientUserId { get; set; }
    public Guid CoachUserId { get; set; }
    public Guid? EventId { get; set; }  // Cross-service reference, no FK
    public string? Comment { get; set; }
    public bool SharedWithPlayer { get; set; }

    public virtual ICollection<ImprovementPoint> ImprovementPoints { get; set; } = new List<ImprovementPoint>();
    public virtual Praise? Praise { get; set; }
}
