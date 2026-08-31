using Coaching.Application.DTOs.Drills;
using Coaching.Domain.Enums;
using Coaching.Domain.Models.Drills;
using Coaching.Domain.Models.Templates;
using Coaching.Application.Services;

namespace Coaching.Application.Interfaces.Services;

/// <summary>
/// The dial half of saving a drill, and the use/value primitives every dial edit shares.
/// Everything stages into the caller's unit of work; nothing here saves.
/// </summary>
public interface IDrillDialReconciler
{
    /// <summary>
    /// Brings the drill's dials to exactly <paramref name="inputs"/>: an input carrying the id
    /// of a current dial updates it (a changed name is a rename and keeps every plan's values),
    /// one without is created (existing plans get its default), and a current dial the list no
    /// longer carries is removed with its values.
    /// </summary>
    Task ReconcileAsync(Drill drill, IReadOnlyList<DrillDialInputDto> inputs);

    Task<List<DrillUse>> LoadUsesAsync(Guid drillId);
    Task<(List<PlanItem> Spine, List<PlanStationItem> Grouped)> LoadUseEntitiesAsync(Guid drillId);
    IEnumerable<DrillUse> AsUses(List<PlanItem> spine, List<PlanStationItem> grouped);
    Task<List<PlanItemDialValue>> ValuesForUsesAsync(IReadOnlyCollection<DrillUse> uses);
    PlanItemDialValue NewValue(DrillUse use, string dialName, string value);
    bool Belongs(PlanItemDialValue value, DrillUse use);
    bool SameUse(PlanItemDialValue left, PlanItemDialValue right);
    void EnsureValidName(string name);
    void EnsureValueFits(string? value);
    void ApplyKindFields(DrillDial dial, DialKind kind, string? defaultValue, string? onText, string? offText, string? onLabel, string? offLabel);
}
