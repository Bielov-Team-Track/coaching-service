using Coaching.Domain.Enums;

namespace Coaching.Application.DTOs.Evaluation;

public class PlayerExerciseScoreDto
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public Guid PlayerId { get; set; }
    public Guid ExerciseId { get; set; }
    public Guid? EvaluatorUserId { get; set; }
    public EvaluationScoreStatus Status { get; set; }
    public DateTime? ScoredAt { get; set; }
    public List<MetricScoreValueDto> MetricScores { get; set; } = new();
}

public class MetricScoreValueDto
{
    public Guid MetricId { get; set; }
    public decimal Value { get; set; }
    public string? Notes { get; set; }
}

public record SubmitExerciseScoresDto
{
    public Guid PlayerId { get; set; }
    public Guid ExerciseId { get; set; }
    public List<MetricScoreValueDto> Scores { get; set; } = new();
}

public class SessionProgressDto
{
    public Guid SessionId { get; set; }
    public EvaluationSessionStatus Status { get; set; }
    public int TotalPlayers { get; set; }
    public int TotalExercises { get; set; }
    public int TotalScored { get; set; }
    public int TotalPossible { get; set; }
    public decimal OverallProgress { get; set; }
    public List<GroupProgressDto> Groups { get; set; } = new();
}

public class GroupProgressDto
{
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public Guid? EvaluatorUserId { get; set; }
    public string? EvaluatorName { get; set; }
    public string? CurrentExerciseName { get; set; }
    public int PlayersScored { get; set; }
    public int TotalPlayers { get; set; }
    public int ExercisesCompleted { get; set; }
    public int TotalExercises { get; set; }
}

public record UpdateSharingDto
{
    public bool? ShareFeedback { get; set; }
    public bool? ShareMetrics { get; set; }
}

public record UpdatePlayerSharingDto
{
    public bool SharedWithPlayer { get; set; }
}
