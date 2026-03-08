using Coaching.Domain.Enums;
using Shared.Models;

namespace Coaching.Domain.Models.Evaluation;

public class EvaluationParticipant : BaseEntity
{
    public Guid EvaluationSessionId { get; set; }
    public Guid PlayerId { get; set; }
    public ParticipantSource Source { get; set; }

    public virtual EvaluationSession Session { get; set; } = null!;
    public virtual PlayerEvaluation? Evaluation { get; set; }
}
