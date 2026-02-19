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

    public virtual EvaluationPlan? EvaluationPlan { get; set; }
    public virtual ICollection<EvaluationParticipant> Participants { get; set; } = new List<EvaluationParticipant>();
}
