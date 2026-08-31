using Coaching.Domain.Enums;
using Shared.Models;

namespace Coaching.Domain.Models.Templates;

/// <summary>
/// Where one activity happens: a court, and a zone inside it when the court is divided.
/// The activity is either a row of the plan or a row inside a station group — exactly one
/// of the two, never both and never neither.
/// <para>
/// The anchor is an id with no foreign key on purpose: saving a plan deletes and recreates
/// every one of its rows, so an anchor can vanish under a placement that is still correct.
/// A read drops those placements from the answer and leaves the rows alone, so a plan edited
/// and then put back finds its floor where it left it.
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
