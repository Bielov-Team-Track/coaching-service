using Coaching.Domain.Enums;
using Shared.Models;

namespace Coaching.Domain.Models.Evaluation;

public class EvaluationSession : BaseEntity
{
    public Guid ClubId { get; set; }
    public Guid? EventId { get; set; }
    public Guid CoachUserId { get; set; }
    public Guid? EvaluationPlanId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public EvaluationSessionStatus Status { get; set; } = EvaluationSessionStatus.Draft;
    public DateTime? StartedAt { get; set; }
    public DateTime? PausedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool ShareFeedback { get; set; }
    public bool ShareMetrics { get; set; }

    public virtual EvaluationPlan? EvaluationPlan { get; set; }
    public virtual ICollection<EvaluationParticipant> Participants { get; set; } = new List<EvaluationParticipant>();
    public virtual ICollection<EvaluationGroup> Groups { get; set; } = new List<EvaluationGroup>();
    public virtual ICollection<PlayerExerciseScore> ExerciseScores { get; set; } = new List<PlayerExerciseScore>();
}
