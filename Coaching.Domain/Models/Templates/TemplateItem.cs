using Coaching.Domain.Models.Drills;
using Shared.Models;

namespace Coaching.Domain.Models.Templates;

public class PlanItem : BaseEntity
{
    public const int NotesMaxLength = 500;

    public Guid TemplateId { get; set; }
    public Guid DrillId { get; set; }
    public Guid? SectionId { get; set; }
    public int Order { get; set; }
    public int Duration { get; set; }
    public string? Notes { get; set; }

    // Navigation properties
    public virtual TrainingPlan Plan { get; set; } = null!;
    public virtual PlanSection? Section { get; set; }
    public virtual Drill? Drill { get; set; }
}
