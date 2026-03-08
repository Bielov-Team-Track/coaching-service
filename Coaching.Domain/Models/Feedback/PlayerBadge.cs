using Coaching.Domain.Enums;
using Shared.Models;

namespace Coaching.Domain.Models.Feedback;

public class PlayerBadge : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid? PraiseId { get; set; }
    public Guid? EventId { get; set; }  // Cross-service reference, no FK
    public BadgeType BadgeType { get; set; }
    public required string Message { get; set; }
    public Guid AwardedByUserId { get; set; }

    public virtual Praise? Praise { get; set; }
}
