using Coaching.Domain.Enums;

namespace Coaching.Application.DTOs.Evaluation;

public class EvaluationThresholdDto
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }
    public VolleyballSkill? Skill { get; set; }
    public decimal MinScore { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
}

public record CreateThresholdDto
{
    public VolleyballSkill? Skill { get; set; }
    public decimal MinScore { get; set; }
    public string? Description { get; set; }
}

public record UpdateThresholdDto
{
    public decimal? MinScore { get; set; }
    public bool? IsActive { get; set; }
    public string? Description { get; set; }
}

public class ThresholdCheckResult
{
    public bool Passed { get; set; }
    public List<SkillThresholdResult> SkillResults { get; set; } = new();
    public string? SuggestedOutcome { get; set; }
}

public class SkillThresholdResult
{
    public VolleyballSkill Skill { get; set; }
    public decimal Score { get; set; }
    public decimal MinRequired { get; set; }
    public bool Passed { get; set; }
}
