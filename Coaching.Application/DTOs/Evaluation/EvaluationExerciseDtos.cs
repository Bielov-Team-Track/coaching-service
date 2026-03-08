using Coaching.Domain.Enums;

namespace Coaching.Application.DTOs.Evaluation;

public class EvaluationExerciseDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public Guid? ClubId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DifficultyLevel Level { get; set; }
    public List<EvaluationMetricDto> Metrics { get; set; } = new();
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class EvaluationMetricDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public MetricType Type { get; set; }
    public decimal MaxPoints { get; set; }
    public string? Config { get; set; }
    public int Order { get; set; }
    public List<MetricSkillWeightDto> SkillWeights { get; set; } = new();
}

public class MetricSkillWeightDto
{
    public Guid Id { get; set; }
    public VolleyballSkill Skill { get; set; }
    public decimal Percentage { get; set; }
}

public record CreateEvaluationExerciseDto
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public Guid? ClubId { get; set; }
    public DifficultyLevel Level { get; set; } = DifficultyLevel.Intermediate;
    public List<CreateEvaluationMetricDto>? Metrics { get; set; }
}

public record CreateEvaluationMetricDto
{
    public required string Name { get; set; }
    public MetricType Type { get; set; }
    public decimal MaxPoints { get; set; }
    public string? Config { get; set; }
    public List<CreateMetricSkillWeightDto> SkillWeights { get; set; } = new();
}

public record CreateMetricSkillWeightDto
{
    public VolleyballSkill Skill { get; set; }
    public decimal Percentage { get; set; }
}

public record UpdateEvaluationExerciseDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public DifficultyLevel? Level { get; set; }
}

public record UpdateEvaluationMetricDto
{
    public string? Name { get; set; }
    public MetricType? Type { get; set; }
    public decimal? MaxPoints { get; set; }
    public string? Config { get; set; }
}

public record AddMetricDto
{
    public required string Name { get; set; }
    public MetricType Type { get; set; }
    public decimal MaxPoints { get; set; }
    public string? Config { get; set; }
    public int? Order { get; set; }
    public List<CreateMetricSkillWeightDto> SkillWeights { get; set; } = new();
}

public class ExerciseListResponseDto
{
    public IEnumerable<EvaluationExerciseDto> Items { get; set; } = new List<EvaluationExerciseDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
