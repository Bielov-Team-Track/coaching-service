using Coaching.Domain.Enums;
using Shared.Models;

namespace Coaching.Domain.Models.Evaluation;

public class PlayerExerciseScore : BaseEntity
{
    public Guid SessionId { get; set; }
    public Guid PlayerId { get; set; }
    public Guid ExerciseId { get; set; }
    public Guid? EvaluatorUserId { get; set; }
    public EvaluationScoreStatus Status { get; set; } = EvaluationScoreStatus.Pending;
    public DateTime? ScoredAt { get; set; }

    public virtual EvaluationSession Session { get; set; } = null!;
    public virtual EvaluationExercise Exercise { get; set; } = null!;
    public virtual ICollection<PlayerMetricScore> MetricScores { get; set; } = new List<PlayerMetricScore>();
}
