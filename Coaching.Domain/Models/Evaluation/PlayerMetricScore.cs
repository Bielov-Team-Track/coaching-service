using Shared.Models;

namespace Coaching.Domain.Models.Evaluation;

public class PlayerMetricScore : BaseEntity
{
    public Guid EvaluationId { get; set; }
    public Guid MetricId { get; set; }
    public decimal RawValue { get; set; }        // What coach entered
    public decimal NormalizedScore { get; set; } // 0-1

    public virtual PlayerEvaluation Evaluation { get; set; } = null!;
    public virtual EvaluationMetric Metric { get; set; } = null!;
}
