using Coaching.Domain.Enums;
using Shared.Models;

namespace Coaching.Domain.Models.Evaluation;

public class MetricSkillWeight : BaseEntity
{
    public Guid MetricId { get; set; }
    public VolleyballSkill Skill { get; set; }
    public decimal Percentage { get; set; }  // 0-100, all weights for a metric must sum to 100

    public virtual EvaluationMetric Metric { get; set; } = null!;
}
