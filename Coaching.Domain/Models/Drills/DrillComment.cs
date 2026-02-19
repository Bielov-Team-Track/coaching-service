using Shared.Models;

namespace Coaching.Domain.Models.Drills;

public class DrillComment : BaseEntity
{
    public Guid DrillId { get; set; }
    public Guid UserId { get; set; }
    public required string Content { get; set; }

    // Self-referencing for replies
    public Guid? ParentCommentId { get; set; }

    // Navigation properties
    public virtual Drill Drill { get; set; } = null!;
    public virtual UserProfile? User { get; set; }
    public virtual DrillComment? ParentComment { get; set; }
    public virtual ICollection<DrillComment> Replies { get; set; } = new List<DrillComment>();
}
