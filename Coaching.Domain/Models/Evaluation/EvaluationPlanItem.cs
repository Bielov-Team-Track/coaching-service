using Shared.Models;

namespace Coaching.Domain.Models.Evaluation;

public class EvaluationPlanItem : BaseEntity
{
    public Guid PlanId { get; set; }
    public Guid ExerciseId { get; set; }
    public int Order { get; set; }

    public virtual EvaluationPlan Plan { get; set; } = null!;
    public virtual EvaluationExercise Exercise { get; set; } = null!;
}
