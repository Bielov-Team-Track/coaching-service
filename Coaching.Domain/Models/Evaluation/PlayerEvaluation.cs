using Coaching.Domain.Enums;
using Shared.Models;

namespace Coaching.Domain.Models.Evaluation;

public class PlayerEvaluation : BaseEntity
{
    public Guid EvaluationParticipantId { get; set; }
    public Guid PlayerId { get; set; }
    public Guid EvaluatedByUserId { get; set; }
    public EvaluationOutcome? Outcome { get; set; }
    public bool SharedWithPlayer { get; set; }
    public string? CoachNotes { get; set; }
    public Guid? SessionId { get; set; }
    public Guid? FeedbackId { get; set; }

    public virtual EvaluationParticipant Participant { get; set; } = null!;
    public virtual EvaluationSession? Session { get; set; }
    public virtual Feedback.Feedback? Feedback { get; set; }
    public virtual ICollection<PlayerMetricScore> MetricScores { get; set; } = new List<PlayerMetricScore>();
    public virtual ICollection<PlayerSkillScore> SkillScores { get; set; } = new List<PlayerSkillScore>();
}
