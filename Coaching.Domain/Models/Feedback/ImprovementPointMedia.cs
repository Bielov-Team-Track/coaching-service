using Coaching.Domain.Enums;
using Shared.Models;

namespace Coaching.Domain.Models.Feedback;

public class ImprovementPointMedia : BaseEntity
{
    public Guid ImprovementPointId { get; set; }
    public required string Url { get; set; }
    public FeedbackMediaType Type { get; set; }
    public string? Title { get; set; }

    public virtual ImprovementPoint ImprovementPoint { get; set; } = null!;
}
