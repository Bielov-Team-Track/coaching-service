using Shared.Models;

namespace Coaching.Domain.Models.Templates;

public class PlanComment : BaseEntity
{
    public Guid TemplateId { get; set; }
    public Guid UserId { get; set; }
    public required string Content { get; set; }
    public Guid? ParentCommentId { get; set; }

    // Navigation properties
    public virtual TrainingPlan Plan { get; set; } = null!;
    public virtual UserProfile? User { get; set; }
    public virtual PlanComment? ParentComment { get; set; }
    public virtual ICollection<PlanComment> Replies { get; set; } = new List<PlanComment>();
}
