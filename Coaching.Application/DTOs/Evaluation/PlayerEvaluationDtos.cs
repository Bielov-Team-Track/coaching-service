using Coaching.Domain.Enums;

namespace Coaching.Application.DTOs.Evaluation;

public class PlayerEvaluationDto
{
    public Guid Id { get; set; }
    public Guid EvaluationParticipantId { get; set; }
    public Guid PlayerId { get; set; }
    public Guid EvaluatedByUserId { get; set; }
    public EvaluationOutcome? Outcome { get; set; }
    public bool SharedWithPlayer { get; set; }
    public string? CoachNotes { get; set; }
    public List<PlayerMetricScoreDto> MetricScores { get; set; } = new();
    public List<PlayerSkillScoreDto> SkillScores { get; set; } = new();
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class PlayerMetricScoreDto
{
    public Guid Id { get; set; }
    public Guid MetricId { get; set; }
    public string MetricName { get; set; } = string.Empty;
    public decimal RawValue { get; set; }
    public decimal NormalizedScore { get; set; }
}

public class PlayerSkillScoreDto
{
    public Guid Id { get; set; }
    public VolleyballSkill Skill { get; set; }
    public decimal EarnedPoints { get; set; }
    public decimal MaxPoints { get; set; }
    public decimal Score { get; set; }
    public string? Level { get; set; }
}

public record CreatePlayerEvaluationDto
{
    public Guid PlayerId { get; set; }
    public string? CoachNotes { get; set; }
}

public record RecordMetricScoreDto
{
    public Guid MetricId { get; set; }
    public decimal RawValue { get; set; }
}

public record RecordMetricScoresDto
{
    public List<RecordMetricScoreDto> Scores { get; set; } = new();
}

public record UpdateEvaluationOutcomeDto
{
    public EvaluationOutcome Outcome { get; set; }
    public string? CoachNotes { get; set; }
}

public class EvaluationSummaryDto
{
    public Guid SessionId { get; set; }
    public int TotalPlayers { get; set; }
    public int EvaluatedCount { get; set; }
    public int PassedCount { get; set; }
    public int FailedCount { get; set; }
    public int PendingCount { get; set; }
    public List<PlayerEvaluationDto> Evaluations { get; set; } = new();
}
