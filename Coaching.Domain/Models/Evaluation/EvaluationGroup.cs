using Shared.Models;

namespace Coaching.Domain.Models.Evaluation;

public class EvaluationGroup : BaseEntity
{
    public Guid SessionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? EvaluatorUserId { get; set; }
    public int Order { get; set; }

    public virtual EvaluationSession Session { get; set; } = null!;
    public virtual ICollection<EvaluationGroupPlayer> Players { get; set; } = new List<EvaluationGroupPlayer>();
}
