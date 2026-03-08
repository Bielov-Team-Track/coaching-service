using Coaching.Domain.Enums;
using Shared.Models;

namespace Coaching.Domain.Models.Evaluation;

public class EvaluationExercise : BaseEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public Guid? ClubId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DifficultyLevel Level { get; set; } = DifficultyLevel.Intermediate;

    public virtual ICollection<EvaluationMetric> Metrics { get; set; } = new List<EvaluationMetric>();
    public virtual ICollection<EvaluationPlanItem> PlanItems { get; set; } = new List<EvaluationPlanItem>();
}
