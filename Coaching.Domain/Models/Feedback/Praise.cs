using Coaching.Domain.Enums;
using Shared.Models;

namespace Coaching.Domain.Models.Feedback;

public class Praise : BaseEntity
{
    public Guid FeedbackId { get; set; }
    public required string Message { get; set; }
    public BadgeType? BadgeType { get; set; }

    public virtual Feedback Feedback { get; set; } = null!;
}
