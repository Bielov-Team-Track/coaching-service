using Coaching.Application.Interfaces.Services;
using Coaching.Domain.Enums;
using Coaching.Domain.Models.Evaluation;
using System.Text.Json;

namespace Coaching.Application.Services;

public class ScoreCalculationService : IScoreCalculationService
{
    public decimal NormalizeMetricValue(EvaluationMetric metric, decimal rawValue)
    {
        return metric.Type switch
        {
            MetricType.Checkbox => rawValue > 0 ? 1m : 0m,
            MetricType.Slider => ClampDecimal(rawValue / 10m, 0m, 1m),
            MetricType.Number => NormalizeNumber(metric, rawValue),
            MetricType.Ratio => ClampDecimal(rawValue, 0m, 1m),
            _ => 0m
        };
    }

    private static decimal ClampDecimal(decimal value, decimal min, decimal max)
    {
        return value < min ? min : (value > max ? max : value);
    }

    private static decimal NormalizeNumber(EvaluationMetric metric, decimal rawValue)
    {
        if (string.IsNullOrEmpty(metric.Config))
            return 0m;

        try
        {
            var config = JsonSerializer.Deserialize<MetricConfig>(metric.Config);
            if (config?.Target == null || config.Target <= 0)
                return 0m;

            var normalized = rawValue / config.Target.Value;
            return normalized < 0m ? 0m : (normalized > 1m ? 1m : normalized);
        }
        catch
        {
            return 0m;
        }
    }

    public Dictionary<VolleyballSkill, decimal> CalculateSkillPoints(PlayerEvaluation evaluation, EvaluationPlan plan)
    {
        var skillPoints = InitializeSkillDictionary();

        foreach (var metricScore in evaluation.MetricScores)
        {
            var metric = metricScore.Metric;
            var pointsEarned = metricScore.NormalizedScore * metric.MaxPoints;

            foreach (var weight in metric.SkillWeights)
            {
                var contribution = pointsEarned * (weight.Percentage / 100m);
                skillPoints[weight.Skill] += contribution;
            }
        }

        return skillPoints;
    }

    public Dictionary<VolleyballSkill, decimal> CalculateMaxSkillPoints(EvaluationPlan plan)
    {
        var maxPoints = InitializeSkillDictionary();

        foreach (var item in plan.Items)
        {
            foreach (var metric in item.Exercise.Metrics)
            {
                foreach (var weight in metric.SkillWeights)
                {
                    var contribution = metric.MaxPoints * (weight.Percentage / 100m);
                    maxPoints[weight.Skill] += contribution;
                }
            }
        }

        return maxPoints;
    }

    public string? GetLevelForScore(decimal score, VolleyballSkill skill, ClubSkillMatrix matrix)
    {
        var matrixSkill = matrix.Skills.FirstOrDefault(s => s.Skill == skill);
        if (matrixSkill == null)
            return null;

        var band = matrixSkill.Bands
            .OrderBy(b => b.MinScore)
            .FirstOrDefault(b => score >= b.MinScore && score <= b.MaxScore);

        return band?.Label;
    }

    private static Dictionary<VolleyballSkill, decimal> InitializeSkillDictionary()
    {
        return Enum.GetValues<VolleyballSkill>()
            .ToDictionary(skill => skill, _ => 0m);
    }

    private class MetricConfig
    {
        public decimal? Target { get; set; }
    }
}
