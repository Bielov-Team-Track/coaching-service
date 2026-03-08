using Coaching.Domain.Enums;
using Shared.Models;

namespace Coaching.Domain.Models.Evaluation;

public class EvaluationMetric : BaseEntity
{
    public Guid ExerciseId { get; set; }
    public required string Name { get; set; }
    public MetricType Type { get; set; }
    public decimal MaxPoints { get; set; }
    public string? Config { get; set; }  // JSON: { "target": 100 } for Number type
    public int Order { get; set; }

    public virtual EvaluationExercise Exercise { get; set; } = null!;
    public virtual ICollection<MetricSkillWeight> SkillWeights { get; set; } = new List<MetricSkillWeight>();
    public virtual ICollection<PlayerMetricScore> PlayerScores { get; set; } = new List<PlayerMetricScore>();
}
