using Coaching.Application.DTOs.Templates;
using Coaching.Application.Interfaces.Repositories;
using Coaching.Application.Interfaces.Services;
using Coaching.Domain.Enums;
using Coaching.Domain.Models.Templates;
using Microsoft.EntityFrameworkCore;
using Shared.DataAccess.Repositories.Interfaces;
using Shared.DTOs.Errors;
using Shared.Enums;
using Shared.Exceptions;

namespace Coaching.Application.Services;

public class PlanFloorService(
    ITrainingPlanRepository planRepository,
    IPlanItemRepository itemRepository,
    IRepository<PlanStationItem> stationItemRepository,
    IRepository<PlanCourtBooking> bookingRepository,
    IRepository<PlanItemPlacement> placementRepository,
    IEventsGrpcClient eventsGrpcClient) : IPlanFloorService
{
    /// <summary>An activity, whichever of the two kinds it is. The natural key of a placement.</summary>
    private readonly record struct Anchor(bool IsStationItem, Guid Id);

    public async Task<PlanFloorDto> GetFloorAsync(Guid planId, Guid venueId, Guid userId)
    {
        EnsureVenue(venueId);

        var plan = await LoadPlanOrThrowAsync(planId);
        await EnsureCanReadAsync(plan, userId);

        var bookings = await LoadBookingsAsync(planId, venueId);
        var placements = await LoadPlacementsAsync(planId, venueId);

        var live = placements;
        if (placements.Count > 0)
        {
            var (itemIds, stationItemIds) = await LoadAnchorIdsAsync(planId);
            live = placements.Where(p => IsLive(p, itemIds, stationItemIds)).ToList();
        }

        return MapFloor(planId, venueId, bookings, live, placements.Count - live.Count);
    }

    public async Task<PlanFloorDto> PutFloorAsync(Guid planId, Guid venueId, SavePlanFloorDto request, Guid userId)
    {
        EnsureVenue(venueId);

        var plan = await LoadPlanOrThrowAsync(planId);

        if (plan.PlanType != PlanType.Instance)
            throw new BadRequestException("A template has no floor", ErrorCodeEnum.ValidationError);

        await EnsureCanEditAsync(plan, userId);

        var incomingBookings = request.Bookings ?? [];
        var incomingPlacements = request.Placements ?? [];

        // Only the placements need the plan's activities, and only to be checked against them.
        var anchors = incomingPlacements.Count > 0
            ? await LoadAnchorIdsAsync(planId)
            : (ItemIds: new HashSet<Guid>(), StationItemIds: new HashSet<Guid>());

        Validate(incomingBookings, incomingPlacements, anchors.ItemIds, anchors.StationItemIds);

        var bookings = await ReconcileBookingsAsync(planId, venueId, incomingBookings);
        var placements = await ReconcilePlacementsAsync(planId, venueId, incomingPlacements);

        await bookingRepository.SaveChangesAsync();

        // Nothing here can be stale: every placement was just checked against the plan.
        return MapFloor(planId, venueId, bookings, placements, 0);
    }

    private static void EnsureVenue(Guid venueId)
    {
        if (venueId == Guid.Empty)
            throw new BadRequestException("A venue is required", ErrorCodeEnum.ValidationError);
    }

    private async Task<TrainingPlan> LoadPlanOrThrowAsync(Guid planId)
    {
        var plan = await planRepository.Query()
            .FirstOrDefaultAsync(p => p.Id == planId && !p.IsDeleted);

        return plan ?? throw new EntityNotFoundException("Plan not found");
    }

    // Mirrors ValidatePlanAccessAsync in TrainingPlanService (owner, or a public plan), widened by
    // the participant rule its GetByEventIdAsync uses and the event-admin check RunService reads
    // runs with: the floor of an event's plan is read by the people standing on it, not only by
    // the coach who wrote it. Duplicated rather than shared because the original is private.
    private async Task EnsureCanReadAsync(TrainingPlan plan, Guid userId)
    {
        if (plan.CreatedByUserId == userId)
            return;

        if (plan.Visibility == TemplateVisibility.Public)
            return;

        if (plan.PlanType == PlanType.Instance && plan.EventId.HasValue)
        {
            var (isParticipant, eventExists) = await eventsGrpcClient.IsEventParticipantAsync(plan.EventId.Value, userId);
            if (!eventExists)
                throw new EntityNotFoundException("Event not found");

            if (isParticipant || await eventsGrpcClient.IsEventAdminAsync(plan.EventId.Value, userId))
                return;
        }

        throw new ForbiddenException("You do not have permission to view this plan");
    }

    // Owner or event admin, the rule PromoteToTemplateAsync already applies to an instance plan.
    private async Task EnsureCanEditAsync(TrainingPlan plan, Guid userId)
    {
        if (plan.CreatedByUserId == userId)
            return;

        if (plan.EventId.HasValue && await eventsGrpcClient.IsEventAdminAsync(plan.EventId.Value, userId))
            return;

        throw new ForbiddenException("Only the plan owner or an event admin can change the floor");
    }

    private static void Validate(
        List<SaveCourtBookingDto> bookings,
        List<SavePlacementDto> placements,
        IReadOnlySet<Guid> itemIds,
        IReadOnlySet<Guid> stationItemIds)
    {
        var errors = new List<FieldError>();
        var splitByCourt = new Dictionary<Guid, CourtSplit>();

        for (var i = 0; i < bookings.Count; i++)
        {
            var booking = bookings[i];

            if (booking.CourtId == Guid.Empty)
                errors.Add(new FieldError($"bookings[{i}].courtId", "REQUIRED", "A booking needs a court"));
            else if (!splitByCourt.TryAdd(booking.CourtId, booking.Split))
                errors.Add(new FieldError($"bookings[{i}].courtId", "DUPLICATE", "This court is already on the floor"));

            if (!Enum.IsDefined(booking.Split))
                errors.Add(new FieldError($"bookings[{i}].split", "INVALID_VALUE", "Unknown court split"));

            if (booking.TakenBy?.Trim().Length > PlanCourtBooking.TakenByMaxLength)
                errors.Add(new FieldError($"bookings[{i}].takenBy", "INVALID_LENGTH",
                    $"A name must be at most {PlanCourtBooking.TakenByMaxLength} characters"));
        }

        var seen = new HashSet<(Anchor Anchor, Guid CourtId, string? ZoneId)>();

        for (var i = 0; i < placements.Count; i++)
        {
            var placement = placements[i];
            var field = $"placements[{i}]";

            if (placement.ItemId.HasValue == placement.StationItemId.HasValue)
            {
                errors.Add(new FieldError(field, "INVALID_ANCHOR",
                    "A placement holds exactly one of itemId and stationItemId"));
                continue;
            }

            var anchor = AnchorOf(placement);
            var belongsToPlan = anchor.IsStationItem
                ? stationItemIds.Contains(anchor.Id)
                : itemIds.Contains(anchor.Id);

            if (!belongsToPlan)
                errors.Add(new FieldError($"{field}.{(anchor.IsStationItem ? "stationItemId" : "itemId")}",
                    "NOT_FOUND", "This activity is not in this plan"));
            else if (!seen.Add((anchor, placement.CourtId, placement.ZoneId)))
                errors.Add(new FieldError(field, "DUPLICATE", "This activity is already on this zone"));

            if (!splitByCourt.ContainsKey(placement.CourtId))
                errors.Add(new FieldError($"{field}.courtId", "NOT_BOOKED",
                    "This court is not on this venue's floor"));
            else if (!CourtZones.IsKnown(placement.ZoneId))
                errors.Add(new FieldError($"{field}.zoneId", "INVALID_VALUE",
                    $"No court has a zone '{placement.ZoneId}'"));
        }

        if (errors.Count > 0)
            throw new ValidationException("The floor could not be saved", errors);
    }

    private async Task<List<PlanCourtBooking>> ReconcileBookingsAsync(
        Guid planId, Guid venueId, List<SaveCourtBookingDto> incoming)
    {
        // Matched rows are updated in place rather than replaced: a delete and an insert of the
        // same court would race the unique index inside one SaveChanges, and this is the shape
        // RunService settled on for the same reason.
        var unmatched = (await LoadBookingsAsync(planId, venueId)).ToDictionary(b => b.CourtId);
        var floor = new List<PlanCourtBooking>(incoming.Count);

        foreach (var dto in incoming)
        {
            if (unmatched.Remove(dto.CourtId, out var row))
            {
                row.IsOurs = dto.IsOurs ?? true;
                row.TakenBy = NullIfBlank(dto.TakenBy);
                row.Split = dto.Split;
                bookingRepository.Update(row);
            }
            else
            {
                row = new PlanCourtBooking
                {
                    PlanId = planId,
                    VenueId = venueId,
                    CourtId = dto.CourtId,
                    IsOurs = dto.IsOurs ?? true,
                    TakenBy = NullIfBlank(dto.TakenBy),
                    Split = dto.Split,
                };
                bookingRepository.Add(row);
            }

            floor.Add(row);
        }

        // Whatever the payload did not name is off the floor.
        foreach (var dropped in unmatched.Values)
            bookingRepository.Delete(dropped);

        return floor;
    }

    private async Task<List<PlanItemPlacement>> ReconcilePlacementsAsync(
        Guid planId, Guid venueId, List<SavePlacementDto> incoming)
    {
        // A placement's identity is the whole tuple — one activity may hold several zones,
        // so there is nothing to update in place: a row either survives as it is or goes.
        // EF orders the deletes before the inserts inside one SaveChanges, the behaviour the
        // dial reconciler already leans on under its unique index.
        var unmatched = (await LoadPlacementsAsync(planId, venueId))
            .ToDictionary(p => (AnchorOf(p), p.CourtId, p.ZoneId));
        var floor = new List<PlanItemPlacement>(incoming.Count);

        foreach (var dto in incoming)
        {
            if (unmatched.Remove((AnchorOf(dto), dto.CourtId, dto.ZoneId), out var row))
            {
                floor.Add(row);
                continue;
            }

            row = new PlanItemPlacement
            {
                PlanId = planId,
                VenueId = venueId,
                CourtId = dto.CourtId,
                ZoneId = dto.ZoneId,
                ItemId = dto.ItemId,
                StationItemId = dto.StationItemId,
            };
            placementRepository.Add(row);
            floor.Add(row);
        }

        foreach (var dropped in unmatched.Values)
            placementRepository.Delete(dropped);

        return floor;
    }

    private Task<List<PlanCourtBooking>> LoadBookingsAsync(Guid planId, Guid venueId) =>
        bookingRepository.Query()
            .Where(b => b.PlanId == planId && b.VenueId == venueId && !b.IsDeleted)
            .ToListAsync();

    private Task<List<PlanItemPlacement>> LoadPlacementsAsync(Guid planId, Guid venueId) =>
        placementRepository.Query()
            .Where(p => p.PlanId == planId && p.VenueId == venueId && !p.IsDeleted)
            .ToListAsync();

    /// <summary>Every activity the plan has: its own rows, and the rows inside its station groups.</summary>
    private async Task<(HashSet<Guid> ItemIds, HashSet<Guid> StationItemIds)> LoadAnchorIdsAsync(Guid planId)
    {
        // Sequential, not Task.WhenAll: both run on the one scoped DbContext, which does not
        // allow two queries at once.
        var itemIds = await itemRepository.Query()
            .Where(i => i.TemplateId == planId && !i.IsDeleted)
            .Select(i => i.Id)
            .ToListAsync();

        var stationItemIds = await stationItemRepository.Query()
            .Where(si => si.Station.Item.TemplateId == planId && !si.IsDeleted)
            .Select(si => si.Id)
            .ToListAsync();

        return (itemIds.ToHashSet(), stationItemIds.ToHashSet());
    }

    private static bool IsLive(PlanItemPlacement placement, IReadOnlySet<Guid> itemIds, IReadOnlySet<Guid> stationItemIds) =>
        placement.ItemId.HasValue
            ? itemIds.Contains(placement.ItemId.Value)
            : placement.StationItemId.HasValue && stationItemIds.Contains(placement.StationItemId.Value);

    private static Anchor AnchorOf(PlanItemPlacement placement) =>
        new(placement.StationItemId.HasValue, placement.ItemId ?? placement.StationItemId!.Value);

    private static Anchor AnchorOf(SavePlacementDto placement) =>
        new(placement.StationItemId.HasValue, placement.ItemId ?? placement.StationItemId!.Value);

    private static string? NullIfBlank(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    private static PlanFloorDto MapFloor(
        Guid planId,
        Guid venueId,
        List<PlanCourtBooking> bookings,
        List<PlanItemPlacement> placements,
        int stalePlacements) => new()
    {
        PlanId = planId,
        VenueId = venueId,

        // Sorted only so a read and a save answer in the same order; the screen draws courts
        // in the venue's own order, which lives in clubs-service.
        Bookings = bookings.OrderBy(b => b.CourtId).Select(b => new PlanCourtBookingDto
        {
            CourtId = b.CourtId,
            IsOurs = b.IsOurs,
            TakenBy = b.TakenBy,
            Split = b.Split,
        }).ToList(),
        Placements = placements.OrderBy(p => p.CourtId).ThenBy(p => p.ZoneId).Select(p => new PlanItemPlacementDto
        {
            CourtId = p.CourtId,
            ZoneId = p.ZoneId,
            ItemId = p.ItemId,
            StationItemId = p.StationItemId,
        }).ToList(),
        StalePlacements = stalePlacements,
    };
}
