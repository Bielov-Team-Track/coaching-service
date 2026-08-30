using Coaching.Domain.Enums;

namespace Coaching.Application.DTOs.Templates;

/// <summary>
/// The floor for one venue: which of its courts are ours tonight, how each is divided, and
/// where every activity happens. A plan keeps one of these per venue it has been held at.
/// </summary>
public class PlanFloorDto
{
    public Guid PlanId { get; set; }
    public Guid VenueId { get; set; }
    public List<PlanCourtBookingDto> Bookings { get; set; } = new();
    public List<PlanItemPlacementDto> Placements { get; set; } = new();

    /// <summary>
    /// Placements whose activity is no longer in the plan. They are left out of
    /// <see cref="Placements"/> and counted here so the screen can say so.
    /// </summary>
    public int StalePlacements { get; set; }
}

public class PlanCourtBookingDto
{
    public Guid CourtId { get; set; }
    public bool IsOurs { get; set; }
    public string? TakenBy { get; set; }
    public CourtSplit Split { get; set; }
}

public class PlanItemPlacementDto
{
    public Guid CourtId { get; set; }

    /// <summary>Null is the court's whole surface.</summary>
    public string? ZoneId { get; set; }

    public Guid? ItemId { get; set; }
    public Guid? StationItemId { get; set; }
}

/// <summary>
/// The whole floor for one venue. What is here is the floor; what is missing is gone. Other
/// venues the plan has been held at are untouched.
/// </summary>
public record SavePlanFloorDto(
    List<SaveCourtBookingDto>? Bookings,
    List<SavePlacementDto>? Placements);

/// <summary><see cref="IsOurs"/> is null when the client did not say; a court we listed is ours by default.</summary>
public record SaveCourtBookingDto(
    Guid CourtId,
    bool? IsOurs,
    string? TakenBy,
    CourtSplit Split);

/// <summary>Exactly one of <see cref="ItemId"/> and <see cref="StationItemId"/> is set.</summary>
public record SavePlacementDto(
    Guid CourtId,
    string? ZoneId,
    Guid? ItemId,
    Guid? StationItemId);
