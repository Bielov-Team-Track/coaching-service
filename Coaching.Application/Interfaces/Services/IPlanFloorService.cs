using Coaching.Application.DTOs.Templates;

namespace Coaching.Application.Interfaces.Services;

/// <summary>
/// The floor of an event's plan: the venue's courts, how they are divided, and where each
/// activity happens. A template has none — it is written before the gym is known.
/// </summary>
public interface IPlanFloorService
{
    /// <summary>
    /// The floor the plan has at this venue. Placements whose activity has since left the plan
    /// are dropped from the answer and counted in <see cref="PlanFloorDto.StalePlacements"/>.
    /// Read = the plan's readers, or anyone at the event.
    /// </summary>
    Task<PlanFloorDto> GetFloorAsync(Guid planId, Guid venueId, Guid userId);

    /// <summary>
    /// Replaces the whole floor at this venue. Other venues are untouched. Plan owner or event admin.
    /// </summary>
    Task<PlanFloorDto> PutFloorAsync(Guid planId, Guid venueId, SavePlanFloorDto request, Guid userId);
}
