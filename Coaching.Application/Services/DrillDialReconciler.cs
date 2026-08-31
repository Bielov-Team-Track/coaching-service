using Coaching.Application.DTOs.Drills;
using Coaching.Application.Interfaces.Repositories;
using Coaching.Application.Interfaces.Services;
using Coaching.Application.RichText;
using Coaching.Domain.Enums;
using Coaching.Domain.Models.Drills;
using Coaching.Domain.Models.Templates;
using Microsoft.EntityFrameworkCore;
using Shared.DataAccess.Repositories.Interfaces;
using Shared.Enums;
using Shared.Exceptions;

namespace Coaching.Application.Services;

/// <summary>Where a drill is used: a row on a plan's spine, or a row inside a station group.</summary>
public readonly record struct DrillUse(Guid Id, Guid PlanId, bool InStationGroup);

/// <summary>
/// The dial half of saving a drill, and the primitives every dial edit shares. The drill editor
/// sends the whole dial list with each save; <see cref="ReconcileAsync"/> stages the difference —
/// births, renames, retunes, removals — together with the per-use values that keep existing
/// plans reading sensibly. Nothing here saves; the caller owns the unit of work.
/// </summary>
public class DrillDialReconciler : IDrillDialReconciler
{
    private readonly IRepository<DrillDial> _dialRepository;
    private readonly IPlanItemRepository _itemRepository;
    private readonly IRepository<PlanStationItem> _stationItemRepository;
    private readonly IRepository<PlanItemDialValue> _valueRepository;

    public DrillDialReconciler(
        IRepository<DrillDial> dialRepository,
        IPlanItemRepository itemRepository,
        IRepository<PlanStationItem> stationItemRepository,
        IRepository<PlanItemDialValue> valueRepository)
    {
        _dialRepository = dialRepository;
        _itemRepository = itemRepository;
        _stationItemRepository = stationItemRepository;
        _valueRepository = valueRepository;
    }

    public async Task ReconcileAsync(Drill drill, IReadOnlyList<DrillDialInputDto> inputs)
    {
        var seen = new HashSet<string>();
        foreach (var input in inputs)
        {
            EnsureValidName(input.Name.Trim());
            if (!seen.Add(input.Name.Trim()))
                throw new BadRequestException($"The dial list names {input.Name.Trim()} twice", ErrorCodeEnum.ValidationError);
        }

        var byId = drill.Dials.ToDictionary(d => d.Id);
        var kept = new HashSet<Guid>();
        var births = new List<(DrillDialInputDto Input, int Order)>();
        var renames = new Dictionary<string, string>();

        for (var i = 0; i < inputs.Count; i++)
        {
            var input = inputs[i];
            var name = input.Name.Trim();
            if (input.Id is Guid id && byId.TryGetValue(id, out var dial))
            {
                kept.Add(id);
                if (dial.Name != name) renames.Add(dial.Name, name);
                ApplyKindFields(dial, input.Kind, input.DefaultValue, input.OnText, input.OffText, input.OnLabel, input.OffLabel);
                dial.Name = name;
                dial.Order = i;
            }
            else
            {
                // An id naming no dial is a dial deleted under this editor's feet; the coach
                // still wants one of this name, so it is simply born again.
                births.Add((input, i));
            }
        }

        var doomed = drill.Dials.Where(d => !kept.Contains(d.Id)).ToList();
        if (births.Count == 0 && doomed.Count == 0 && renames.Count == 0) return;

        var uses = await LoadUsesAsync(drill.Id);
        var values = await ValuesForUsesAsync(uses);

        RenameValues(values, renames);

        foreach (var dial in doomed)
        {
            foreach (var value in values.Where(v => v.DialName == dial.Name).ToList())
            {
                _valueRepository.Delete(value);
                values.Remove(value);
            }
            _dialRepository.Delete(dial);
        }

        foreach (var (input, order) in births)
        {
            var name = input.Name.Trim();
            var dial = new DrillDial { DrillId = drill.Id, Name = name, Order = order };
            ApplyKindFields(dial, input.Kind, input.DefaultValue, input.OnText, input.OffText, input.OnLabel, input.OffLabel);
            _dialRepository.Add(dial);

            // Every plan already using this drill gets the default, so the coach opens an
            // existing plan and finds the new dial set rather than blank.
            foreach (var use in uses.Where(u => !values.Any(v => Belongs(v, u) && v.DialName == name)))
            {
                var value = NewValue(use, name, dial.DefaultValue);
                _valueRepository.Add(value);
                values.Add(value);
            }
        }
    }

    /// <summary>
    /// Moves values to their dials' new names, every rename read against the row's original
    /// name so two dials swapping names cannot chain. A use can still hold a value under a new
    /// name, left by a dial removed earlier; the live dial's answer takes that row's place.
    /// </summary>
    private void RenameValues(List<PlanItemDialValue> values, IReadOnlyDictionary<string, string> renames)
    {
        if (renames.Count == 0) return;

        var moving = values.Where(v => renames.ContainsKey(v.DialName)).ToList();
        var staying = values.Where(v => !renames.ContainsKey(v.DialName)).ToList();

        foreach (var row in moving)
        {
            var next = renames[row.DialName];
            var stale = staying.FirstOrDefault(v => v.DialName == next && SameUse(v, row));
            if (stale is not null)
            {
                stale.Value = row.Value;
                _valueRepository.Delete(row);
                values.Remove(row);
            }
            else
            {
                row.DialName = next;
            }
        }
    }

    public async Task<List<DrillUse>> LoadUsesAsync(Guid drillId)
    {
        var (spine, grouped) = await LoadUseEntitiesAsync(drillId);
        return AsUses(spine, grouped).ToList();
    }

    public async Task<(List<PlanItem> Spine, List<PlanStationItem> Grouped)> LoadUseEntitiesAsync(Guid drillId)
    {
        var spine = await _itemRepository.Query()
            .Where(i => i.DrillId == drillId && !i.IsDeleted)
            .ToListAsync();

        // A group's rows live in their own table, and a drill is used just as much from inside
        // one — the plan they belong to is two hops up.
        var grouped = await _stationItemRepository.Query()
            .Where(r => r.DrillId == drillId && !r.IsDeleted)
            .Include(r => r.Station)
                .ThenInclude(s => s.Item)
            .ToListAsync();

        return (spine, grouped);
    }

    public IEnumerable<DrillUse> AsUses(List<PlanItem> spine, List<PlanStationItem> grouped) =>
        spine.Select(i => new DrillUse(i.Id, i.TemplateId, false))
            .Concat(grouped.Select(r => new DrillUse(r.Id, r.Station.Item.TemplateId, true)));

    public async Task<List<PlanItemDialValue>> ValuesForUsesAsync(IReadOnlyCollection<DrillUse> uses)
    {
        if (uses.Count == 0) return [];

        var itemIds = uses.Where(u => !u.InStationGroup).Select(u => u.Id).ToList();
        var stationItemIds = uses.Where(u => u.InStationGroup).Select(u => u.Id).ToList();

        return await _valueRepository.Query()
            .Where(v => (v.ItemId != null && itemIds.Contains(v.ItemId.Value))
                     || (v.StationItemId != null && stationItemIds.Contains(v.StationItemId.Value)))
            .ToListAsync();
    }

    public PlanItemDialValue NewValue(DrillUse use, string dialName, string value) => new()
    {
        PlanId = use.PlanId,
        ItemId = use.InStationGroup ? null : use.Id,
        StationItemId = use.InStationGroup ? use.Id : null,
        DialName = dialName,
        Value = value,
    };

    public bool Belongs(PlanItemDialValue value, DrillUse use) =>
        use.InStationGroup ? value.StationItemId == use.Id : value.ItemId == use.Id;

    public bool SameUse(PlanItemDialValue left, PlanItemDialValue right) =>
        left.ItemId == right.ItemId && left.StationItemId == right.StationItemId;

    public void EnsureValidName(string name)
    {
        if (!DialTokens.IsValidName(name))
            throw new BadRequestException(
                $"'{name}' cannot be a dial name: it has to start with a lower-case letter and carry only letters and digits",
                ErrorCodeEnum.ValidationError);
    }

    public void EnsureValueFits(string? value)
    {
        if (value?.Length > DrillDial.ValueMaxLength)
            throw new BadRequestException(
                $"A dial value is longer than {DrillDial.ValueMaxLength} characters",
                ErrorCodeEnum.ValidationError);
    }

    /// <summary>
    /// A kind decides which fields mean anything. A Toggle carries the two sentences it swaps
    /// between; every other kind carries none of them, so a kind change cannot leave a stale
    /// sentence behind for the splice to find.
    /// </summary>
    public void ApplyKindFields(
        DrillDial dial, DialKind kind, string? defaultValue, string? onText, string? offText, string? onLabel, string? offLabel)
    {
        EnsureValueFits(defaultValue);
        EnsureValueFits(onText);
        EnsureValueFits(offText);

        if (onLabel?.Length > DrillDial.LabelMaxLength || offLabel?.Length > DrillDial.LabelMaxLength)
            throw new BadRequestException(
                $"A dial label is longer than {DrillDial.LabelMaxLength} characters",
                ErrorCodeEnum.ValidationError);

        dial.Kind = kind;

        if (kind == DialKind.Toggle)
        {
            if (string.IsNullOrWhiteSpace(onText) || string.IsNullOrWhiteSpace(offText))
                throw new BadRequestException(
                    "A toggle needs the sentence it reads when it is on and the one when it is off",
                    ErrorCodeEnum.ValidationError);

            dial.DefaultValue = bool.TryParse(defaultValue, out var on) && on ? "true" : "false";
            dial.OnText = onText;
            dial.OffText = offText;
            dial.OnLabel = onLabel;
            dial.OffLabel = offLabel;
            return;
        }

        dial.DefaultValue = defaultValue ?? string.Empty;
        dial.OnText = null;
        dial.OffText = null;
        dial.OnLabel = null;
        dial.OffLabel = null;
    }
}
