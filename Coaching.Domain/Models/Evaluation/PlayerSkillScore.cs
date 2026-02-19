using Coaching.Domain.Enums;
using Shared.Models;

namespace Coaching.Domain.Models.Evaluation;

public class PlayerSkillScore : BaseEntity
{
    public Guid EvaluationId { get; set; }
    public VolleyballSkill Skill { get; set; }
    public decimal EarnedPoints { get; set; }
    public decimal MaxPoints { get; set; }
    public decimal Score { get; set; }      // 0-10
    public string? Level { get; set; }      // From matrix lookup

    public virtual PlayerEvaluation Evaluation { get; set; } = null!;
}
