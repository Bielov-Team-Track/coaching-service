using Coaching.Application.DTOs.Drills;

namespace Coaching.Application.Interfaces.Services;

/// <summary>
/// The dials of a drill, and the one operation that spans two drills. Every method here writes
/// the drill's instructions and the recorded values of every plan that uses it in a single save:
/// a dial that exists but no plan has a value for, or a value under a name no dial has, is the
/// state this service exists to make unreachable.
/// </summary>
public interface IDrillDialService
{
    Task<DrillDto> AddAsync(Guid drillId, CreateDrillDialDto request, Guid userId);
    Task<DrillDto> UpdateAsync(Guid drillId, string name, UpdateDrillDialDto request, Guid userId);
    Task<DrillDto> DeleteAsync(Guid drillId, string name, DeleteDrillDialDto request, Guid userId);
    Task<FoldDrillResultDto> FoldAsync(Guid keepDrillId, FoldDrillDto request, Guid userId);
}
