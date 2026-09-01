using Coaching.Domain.Enums;
using Shared.Models;

namespace Coaching.Domain.Models.Templates;

/// <summary>
/// Where one activity happens: a court, and a zone inside it when the court is divided.
/// The activity is either a row of the plan or a row inside a station group — exactly one
/// of the two, never both and never neither.
/// <para>
/// The anchor is an id with no foreign key on purpose: the activity is one of two tables, which
/// one column cannot point at. A save keeps the id of every row it is given back, so a placement
/// survives an edit of the plan; a row the save drops leaves its placement behind, and a read
/// drops those from the answer rather than deleting them.
/// </para>
/// </summary>
public class PlanItemPlacement : BaseEntity
{
    public const int ZoneIdMaxLength = 4;

    public Guid PlanId { get; set; }

    /// <summary>The venue this placement is on. Cross-service ref to clubs-service, no FK.</summary>
    public Guid VenueId { get; set; }

    /// <summary>One of that venue's courts. Cross-service ref to clubs-service, no FK.</summary>
    public Guid CourtId { get; set; }

    /// <summary>Null is the court's whole surface; otherwise one of <see cref="CourtZones"/>.</summary>
    public string? ZoneId { get; set; }

    /// <summary>A TemplateItems id. No FK — see the type summary.</summary>
    public Guid? ItemId { get; set; }

    /// <summary>A PlanStationItems id. No FK — see the type summary.</summary>
    public Guid? StationItemId { get; set; }

    // Navigation properties
    public virtual TrainingPlan Plan { get; set; } = null!;
}
