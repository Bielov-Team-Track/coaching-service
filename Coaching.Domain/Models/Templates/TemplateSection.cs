using Shared.Models;

namespace Coaching.Domain.Models.Templates;

public class PlanSection : BaseEntity
{
    public Guid TemplateId { get; set; }
    public required string Name { get; set; }
    public int Order { get; set; }

    // Navigation properties
    public virtual TrainingPlan Plan { get; set; } = null!;
    public virtual ICollection<PlanItem> Items { get; set; } = new List<PlanItem>();
}
