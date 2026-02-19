using Shared.Models;

namespace Coaching.Domain.Models.Templates;

public class PlanBookmark : BaseEntity
{
    public Guid TemplateId { get; set; }
    public Guid UserId { get; set; }

    // Navigation properties
    public virtual TrainingPlan Plan { get; set; } = null!;
}
