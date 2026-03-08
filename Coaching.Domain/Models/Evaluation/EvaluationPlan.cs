using Shared.Models;

namespace Coaching.Domain.Models.Evaluation;

public class EvaluationPlan : BaseEntity
{
    public Guid? ClubId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string? Name { get; set; }
    public string? Notes { get; set; }

    public virtual ICollection<EvaluationPlanItem> Items { get; set; } = new List<EvaluationPlanItem>();
    public virtual ICollection<EvaluationSession> Sessions { get; set; } = new List<EvaluationSession>();
}
