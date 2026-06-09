using Coaching.Domain.Enums;
using Shared.Models;

namespace Coaching.Domain.Models.Feedback;

public class FeedbackMedia : BaseEntity
{
    public Guid FeedbackId { get; set; }
    public required string Url { get; set; }
    public FeedbackMediaType Type { get; set; }
    public string? Title { get; set; }
    public int Order { get; set; }

    public virtual Feedback Feedback { get; set; } = null!;
}
