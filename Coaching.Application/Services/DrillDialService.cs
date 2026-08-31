using Coaching.Application.DTOs.Drills;
using Coaching.Application.Interfaces.Repositories;
using Coaching.Application.Interfaces.Services;
using Coaching.Application.RichText;
using Coaching.Domain.Enums;
using Coaching.Domain.Models.Drills;
using Coaching.Domain.Models.Feedback;
using Coaching.Domain.Models.Templates;
using Microsoft.EntityFrameworkCore;
using Shared.DataAccess.Repositories.Interfaces;
using Shared.Enums;
using Shared.Exceptions;

namespace Coaching.Application.Services;

public class DrillDialService : IDrillDialService
{
    private readonly IDrillRepository _drillRepository;
    private readonly IRepository<DrillDial> _dialRepository;
    private readonly IPlanItemRepository _itemRepository;
    private readonly IRepository<PlanStationItem> _stationItemRepository;
    private readonly IRepository<PlanItemDialValue> _valueRepository;
    private readonly IRepository<DrillVariation> _variationRepository;
    private readonly IRepository<ImprovementPointDrill> _pointDrillRepository;
    private readonly IClubsGrpcClient _clubsClient;
    private readonly IDrillService _drillService;

    public DrillDialService(
        IDrillRepository drillRepository,
        IRepository<DrillDial> dialRepository,
        IPlanItemRepository itemRepository,
        IRepository<PlanStationItem> stationItemRepository,
        IRepository<PlanItemDialValue> valueRepository,
        IRepository<DrillVariation> variationRepository,
        IRepository<ImprovementPointDrill> pointDrillRepository,
        IClubsGrpcClient clubsClient,
        IDrillService drillService)
    {
        _drillRepository = drillRepository;
        _dialRepository = dialRepository;
        _itemRepository = itemRepository;
        _stationItemRepository = stationItemRepository;
        _valueRepository = valueRepository;
        _variationRepository = variationRepository;
        _pointDrillRepository = pointDrillRepository;
        _clubsClient = clubsClient;
        _drillService = drillService;
    }

    public async Task<DrillDto> AddAsync(Guid drillId, CreateDrillDialDto request, Guid userId)
    {
        var drill = await LoadForEditAsync(drillId, userId);

        var name = request.Name?.Trim() ?? string.Empty;
        EnsureValidName(name);

        if (drill.Dials.Any(d => d.Name == name))
            throw new BadRequestException($"This drill already has a dial called {name}", ErrorCodeEnum.ValidationError);

        var dial = new DrillDial
        {
            DrillId = drill.Id,
            Name = name,
            Kind = request.Kind,
            Order = drill.Dials.Count == 0 ? 0 : drill.Dials.Max(d => d.Order) + 1,
        };
        ApplyKindFields(dial, request.Kind, request.DefaultValue, request.OnText, request.OffText, request.OnLabel, request.OffLabel);

        WriteInstructions(drill, request.InstructionsHtml, drill.Dials.Select(d => d.Name).Append(name));

        _dialRepository.Add(dial);

        // Every plan already using this drill gets the default, so the coach opens an existing
        // plan and finds the new dial set rather than blank.
        var uses = await LoadUsesAsync(drill.Id);
        var already = await ValuesForUsesAsync(uses);
        foreach (var use in uses.Where(u => !already.Any(v => Belongs(v, u) && v.DialName == name)))
            _valueRepository.Add(NewValue(use, name, dial.DefaultValue));

        await _drillRepository.SaveChangesAsync();
        return await ReadAsync(drillId, userId);
    }

    public async Task<DrillDto> UpdateAsync(Guid drillId, string name, UpdateDrillDialDto request, Guid userId)
    {
        var drill = await LoadForEditAsync(drillId, userId);
        var dial = FindDial(drill, name);

        var newName = request.NewName?.Trim();
        var renaming = !string.IsNullOrEmpty(newName) && newName != dial.Name;
        if (renaming)
        {
            EnsureValidName(newName!);
            if (drill.Dials.Any(d => d.Id != dial.Id && d.Name == newName))
                throw new BadRequestException($"This drill already has a dial called {newName}", ErrorCodeEnum.ValidationError);

            if (request.InstructionsHtml is null)
                throw new BadRequestException("A rename has to bring the re-tokenized instructions with it", ErrorCodeEnum.ValidationError);
        }

        ApplyKindFields(
            dial,
            dial.Kind,
            request.DefaultValue ?? dial.DefaultValue,
            request.OnText ?? dial.OnText,
            request.OffText ?? dial.OffText,
            request.OnLabel ?? dial.OnLabel,
            request.OffLabel ?? dial.OffLabel);

        if (request.InstructionsHtml is not null)
        {
            var names = drill.Dials.Select(d => d.Id == dial.Id && renaming ? newName! : d.Name);
            WriteInstructions(drill, request.InstructionsHtml, names);
        }

        if (renaming)
        {
            await RenameValuesAsync(drill.Id, dial.Name, newName!);
            dial.Name = newName!;
        }

        await _drillRepository.SaveChangesAsync();
        return await ReadAsync(drillId, userId);
    }

    public async Task<DrillDto> DeleteAsync(Guid drillId, string name, DeleteDrillDialDto request, Guid userId)
    {
        var drill = await LoadForEditAsync(drillId, userId);
        var dial = FindDial(drill, name);

        WriteInstructions(drill, request.InstructionsHtml, drill.Dials.Where(d => d.Id != dial.Id).Select(d => d.Name));

        var uses = await LoadUsesAsync(drill.Id);
        foreach (var value in (await ValuesForUsesAsync(uses)).Where(v => v.DialName == dial.Name))
            _valueRepository.Delete(value);

        _dialRepository.Delete(dial);

        await _drillRepository.SaveChangesAsync();
        return await ReadAsync(drillId, userId);
    }

    public async Task<FoldDrillResultDto> FoldAsync(Guid keepDrillId, FoldDrillDto request, Guid userId)
    {
        if (keepDrillId == request.SourceDrillId)
            throw new BadRequestException("A drill cannot be folded into itself", ErrorCodeEnum.ValidationError);

        // Both drills change, so both have to be the caller's to change.
        var keep = await LoadForEditAsync(keepDrillId, userId);
        var source = await LoadForEditAsync(request.SourceDrillId, userId);

        var supplied = request.ValuesForSourceUses ?? [];
        foreach (var (dialName, value) in supplied)
        {
            EnsureValidName(dialName);
            EnsureValueFits(value);
        }

        var (spine, grouped) = await LoadUseEntitiesAsync(source.Id);
        var uses = AsUses(spine, grouped).ToList();
        var existing = await ValuesForUsesAsync(uses);

        foreach (var item in spine) item.DrillId = keep.Id;
        foreach (var row in grouped) row.DrillId = keep.Id;

        // A use that already answered for one of the keeper's dials keeps its own answer; the
        // supplied values are for the ones it has never been asked.
        foreach (var use in uses)
            foreach (var (dialName, value) in supplied)
                if (!existing.Any(v => Belongs(v, use) && v.DialName == dialName))
                    _valueRepository.Add(NewValue(use, dialName, value));

        await RepointBlockingReferencesAsync(source.Id, keep.Id);

        // The same hard delete the drill editor does; the uses have already moved off it, so
        // nothing is left pointing here for the database to refuse.
        _drillRepository.Delete(source);

        await _drillRepository.SaveChangesAsync();
        return new FoldDrillResultDto(uses.Count);
    }

    /// <summary>
    /// Deleting a drill is refused while feedback or another drill's variation list still names
    /// it. A fold is a merge, so those move to the keeper rather than being dropped — a coach's
    /// improvement point should survive the two drills becoming one.
    /// </summary>
    private async Task RepointBlockingReferencesAsync(Guid sourceId, Guid keepId)
    {
        var variations = await _variationRepository.Query()
            .Where(v => v.TargetDrillId == sourceId)
            .ToListAsync();

        var keepersOwn = await _variationRepository.Query()
            .Where(v => v.TargetDrillId == keepId)
            .Select(v => v.SourceDrillId)
            .ToListAsync();

        foreach (var variation in variations)
        {
            // A drill is not a variation of itself, and it is not one twice.
            if (variation.SourceDrillId == keepId || keepersOwn.Contains(variation.SourceDrillId))
                _variationRepository.Delete(variation);
            else
                variation.TargetDrillId = keepId;
        }

        var links = await _pointDrillRepository.Query()
            .Where(l => l.DrillId == sourceId)
            .ToListAsync();

        var pointsAlreadyOnKeeper = await _pointDrillRepository.Query()
            .Where(l => l.DrillId == keepId)
            .Select(l => l.ImprovementPointId)
            .ToListAsync();

        foreach (var link in links)
        {
            if (pointsAlreadyOnKeeper.Contains(link.ImprovementPointId))
                _pointDrillRepository.Delete(link);
            else
                link.DrillId = keepId;
        }
    }

    private async Task RenameValuesAsync(Guid drillId, string oldName, string newName)
    {
        var uses = await LoadUsesAsync(drillId);
        if (uses.Count == 0) return;

        var rows = await ValuesForUsesAsync(uses);

        foreach (var row in rows.Where(v => v.DialName == oldName).ToList())
        {
            // A use can still hold a value under the new name, left by a dial removed earlier.
            // Two rows cannot share a name on one use, and the live dial's answer is the one
            // that means anything — so it takes the stale row's place rather than colliding
            // with it mid-save.
            var stale = rows.FirstOrDefault(v => v.DialName == newName && SameUse(v, row));
            if (stale is not null)
            {
                stale.Value = row.Value;
                _valueRepository.Delete(row);
            }
            else
            {
                row.DialName = newName;
            }
        }
    }

    private async Task<Drill> LoadForEditAsync(Guid drillId, Guid userId)
    {
        var drill = await _drillRepository.GetByIdWithDetailsAsync(drillId);
        if (drill == null)
            throw new EntityNotFoundException("Drill not found");

        await DrillEditRules.EnsureCanEditAsync(drill, userId, _clubsClient);
        return drill;
    }

    private async Task<DrillDto> ReadAsync(Guid drillId, Guid userId) =>
        await _drillService.GetByIdAsync(drillId, userId)
        ?? throw new EntityNotFoundException("Drill not found");

    private static DrillDial FindDial(Drill drill, string name) =>
        drill.Dials.FirstOrDefault(d => d.Name == name)
        ?? throw new EntityNotFoundException($"This drill has no dial called {name}");

    /// <summary>
    /// Stores the client's re-tokenized prose, but only once it agrees with the dials it is
    /// stored beside. A token with no dial renders as a literal brace on the coach's screen;
    /// a dial with no token is a control that changes nothing.
    /// </summary>
    private static void WriteInstructions(Drill drill, string html, IEnumerable<string> dialNames)
    {
        var resolved = DrillRichText.Resolve(html, null, ordered: true);
        var (unknown, unused) = DialTokens.Reconcile(resolved.Lines, dialNames);

        if (unknown.Count > 0)
            throw new BadRequestException(
                $"The instructions mention {{{unknown[0]}}}, which is not a dial on this drill",
                ErrorCodeEnum.ValidationError);

        if (unused.Count > 0)
            throw new BadRequestException(
                $"The instructions no longer mention {{{unused[0]}}}",
                ErrorCodeEnum.ValidationError);

        drill.InstructionsHtml = resolved.Html;
        drill.Instructions = resolved.Lines;
    }

    private static void EnsureValidName(string name)
    {
        if (!DialTokens.IsValidName(name))
            throw new BadRequestException(
                $"'{name}' cannot be a dial name: it has to start with a letter and carry only letters and digits",
                ErrorCodeEnum.ValidationError);
    }

    private static void EnsureValueFits(string? value)
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
    private static void ApplyKindFields(
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

    /// <summary>Where a drill is used: a row on a plan's spine, or a row inside a station group.</summary>
    private readonly record struct DrillUse(Guid Id, Guid PlanId, bool InStationGroup);

    private async Task<List<DrillUse>> LoadUsesAsync(Guid drillId)
    {
        var (spine, grouped) = await LoadUseEntitiesAsync(drillId);
        return AsUses(spine, grouped).ToList();
    }

    private async Task<(List<PlanItem> Spine, List<PlanStationItem> Grouped)> LoadUseEntitiesAsync(Guid drillId)
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

    private static IEnumerable<DrillUse> AsUses(List<PlanItem> spine, List<PlanStationItem> grouped) =>
        spine.Select(i => new DrillUse(i.Id, i.TemplateId, false))
            .Concat(grouped.Select(r => new DrillUse(r.Id, r.Station.Item.TemplateId, true)));

    private async Task<List<PlanItemDialValue>> ValuesForUsesAsync(IReadOnlyCollection<DrillUse> uses)
    {
        if (uses.Count == 0) return [];

        var itemIds = uses.Where(u => !u.InStationGroup).Select(u => u.Id).ToList();
        var stationItemIds = uses.Where(u => u.InStationGroup).Select(u => u.Id).ToList();

        return await _valueRepository.Query()
            .Where(v => (v.ItemId != null && itemIds.Contains(v.ItemId.Value))
                     || (v.StationItemId != null && stationItemIds.Contains(v.StationItemId.Value)))
            .ToListAsync();
    }

    private static PlanItemDialValue NewValue(DrillUse use, string dialName, string value) => new()
    {
        PlanId = use.PlanId,
        ItemId = use.InStationGroup ? null : use.Id,
        StationItemId = use.InStationGroup ? use.Id : null,
        DialName = dialName,
        Value = value,
    };

    private static bool Belongs(PlanItemDialValue value, DrillUse use) =>
        use.InStationGroup ? value.StationItemId == use.Id : value.ItemId == use.Id;

    private static bool SameUse(PlanItemDialValue left, PlanItemDialValue right) =>
        left.ItemId == right.ItemId && left.StationItemId == right.StationItemId;
}
