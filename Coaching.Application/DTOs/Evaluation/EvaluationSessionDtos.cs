using Coaching.Domain.Enums;

namespace Coaching.Application.DTOs.Evaluation;

public class EvaluationSessionDto
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }
    public Guid? EventId { get; set; }
    public Guid CoachUserId { get; set; }
    public Guid? EvaluationPlanId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public EvaluationSessionStatus Status { get; set; }
    public EvaluationPlanDto? EvaluationPlan { get; set; }
    public List<EvaluationParticipantDto> Participants { get; set; } = new();
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public record CreateEvaluationSessionDto
{
    public Guid ClubId { get; set; }
    public Guid? EventId { get; set; }
    public Guid? EvaluationPlanId { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
}

public record UpdateEvaluationSessionDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public Guid? EvaluationPlanId { get; set; }
    public EvaluationSessionStatus? Status { get; set; }
}
