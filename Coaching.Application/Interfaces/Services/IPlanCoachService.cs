using Coaching.Application.DTOs.Templates;

namespace Coaching.Application.Interfaces.Services;

/// <summary>
/// Assigning coaches to an event's practice: the whole plan, or one station within it.
/// </summary>
public interface IPlanCoachService
{
    /// <summary>Replaces the plan's coach set and returns it, names resolved.</summary>
    Task<IReadOnlyList<PlanCoachDto>> ReplacePlanCoachesAsync(Guid planId, AssignCoachesDto request, Guid userId);

    /// <summary>Replaces one station's coach set and returns it, names resolved.</summary>
    Task<IReadOnlyList<PlanCoachDto>> ReplaceStationCoachesAsync(Guid planId, Guid stationId, AssignCoachesDto request, Guid userId);

    /// <summary>
    /// Fills in names and avatars for coach rows already mapped onto a DTO, in one query for
    /// the whole batch. Callers pass every coach on the plan at once — the plan's and all its
    /// stations' — so a plan with ten stations still costs one lookup.
    /// </summary>
    Task ResolveNamesAsync(IReadOnlyCollection<PlanCoachDto> coaches);
}
