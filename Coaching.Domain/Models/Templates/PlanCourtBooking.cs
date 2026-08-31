using Coaching.Domain.Enums;
using Shared.Models;

namespace Coaching.Domain.Models.Templates;

/// <summary>
/// One of the venue's courts on the night this plan is for: whether it is ours, who has it
/// if it is not, and how we have divided it. Only an event's plan has any — a template is
/// written before anyone knows which gym it will be run in.
/// </summary>
public class PlanCourtBooking : BaseEntity
{
    public const int TakenByMaxLength = 100;

    public Guid PlanId { get; set; }

    /// <summary>The venue whose floor this is. Cross-service ref to clubs-service, no FK.</summary>
    public Guid VenueId { get; set; }

    /// <summary>One of that venue's courts. Cross-service ref to clubs-service, no FK.</summary>
    public Guid CourtId { get; set; }

    public bool IsOurs { get; set; } = true;

    /// <summary>Who has the court when it is not ours. A name the coach types; nothing resolves it.</summary>
    public string? TakenBy { get; set; }

    public CourtSplit Split { get; set; } = CourtSplit.Full;

    // Navigation properties
    public virtual TrainingPlan Plan { get; set; } = null!;
}
