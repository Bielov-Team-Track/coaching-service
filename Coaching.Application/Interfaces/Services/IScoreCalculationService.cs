using Coaching.Domain.Enums;
using Coaching.Domain.Models.Evaluation;

namespace Coaching.Application.Interfaces.Services;

public interface IScoreCalculationService
{
    decimal NormalizeMetricValue(EvaluationMetric metric, decimal rawValue);
    Dictionary<VolleyballSkill, decimal> CalculateSkillPoints(PlayerEvaluation evaluation, EvaluationPlan plan);
    Dictionary<VolleyballSkill, decimal> CalculateMaxSkillPoints(EvaluationPlan plan);
    string? GetLevelForScore(decimal score, VolleyballSkill skill, ClubSkillMatrix matrix);
}
