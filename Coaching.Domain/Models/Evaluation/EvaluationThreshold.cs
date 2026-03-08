using Coaching.Domain.Enums;
using Shared.Models;

namespace Coaching.Domain.Models.Evaluation;

public class EvaluationThreshold : BaseEntity
{
    public Guid ClubId { get; set; }
    public VolleyballSkill? Skill { get; set; }  // null = overall threshold
    public decimal MinScore { get; set; }        // Min score to pass
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
    public Guid CreatedByUserId { get; set; }
}
