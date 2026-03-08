using Shared.Models;

namespace Coaching.Domain.Models.Evaluation;

public class EvaluationGroupPlayer : BaseEntity
{
    public Guid GroupId { get; set; }
    public Guid PlayerId { get; set; }

    public virtual EvaluationGroup Group { get; set; } = null!;
}
