using Coaching.Application.DTOs.Templates;
using Coaching.Application.Interfaces.Repositories;
using Coaching.Application.Interfaces.Services;
using Coaching.Domain.Enums;
using Coaching.Domain.Models.Templates;
using Microsoft.EntityFrameworkCore;
using Shared.DataAccess.Repositories.Interfaces;
using Shared.Enums;
using Shared.Exceptions;
using Shared.Models;

namespace Coaching.Application.Services;

public class PlanCoachService : IPlanCoachService
{
    private readonly ITrainingPlanRepository _planRepository;
    private readonly IRepository<PlanCoach> _planCoachRepository;
    private readonly IRepository<PlanStation> _stationRepository;
    private readonly IRepository<PlanStationCoach> _stationCoachRepository;
    private readonly IRepository<UserProfile> _userProfileRepository;
    private readonly IEventsGrpcClient _eventsGrpcClient;

    public PlanCoachService(
        ITrainingPlanRepository planRepository,
        IRepository<PlanCoach> planCoachRepository,
        IRepository<PlanStation> stationRepository,
        IRepository<PlanStationCoach> stationCoachRepository,
        IRepository<UserProfile> userProfileRepository,
        IEventsGrpcClient eventsGrpcClient)
    {
        _planRepository = planRepository;
        _planCoachRepository = planCoachRepository;
        _stationRepository = stationRepository;
        _stationCoachRepository = stationCoachRepository;
        _userProfileRepository = userProfileRepository;
        _eventsGrpcClient = eventsGrpcClient;
    }

    public async Task<IReadOnlyList<PlanCoachDto>> ReplacePlanCoachesAsync(Guid planId, AssignCoachesDto request, Guid userId)
    {
        var plan = await LoadAssignablePlanAsync(planId, userId);
        var userIds = await ValidateCoachesAsync(plan, request);

        var existing = await _planCoachRepository.Query()
            .Where(c => c.PlanId == planId)
            .ToListAsync();

        foreach (var removed in existing.Where(c => !userIds.Contains(c.UserId)))
            _planCoachRepository.Delete(removed);

        foreach (var added in userIds.Where(id => existing.All(c => c.UserId != id)))
            _planCoachRepository.Add(new PlanCoach { PlanId = planId, UserId = added });

        await _planCoachRepository.SaveChangesAsync();

        return await ToResolvedDtosAsync(userIds);
    }

    public async Task<IReadOnlyList<PlanCoachDto>> ReplaceStationCoachesAsync(Guid planId, Guid stationId, AssignCoachesDto request, Guid userId)
    {
        var plan = await LoadAssignablePlanAsync(planId, userId);

        // A station reaches its plan through the Stations row that owns it. Checking the whole
        // path is what stops a caller with rights on their own plan from editing someone else's
        // station by id.
        var stationExists = await _stationRepository.Query()
            .AnyAsync(s => s.Id == stationId && s.Item.TemplateId == planId);
        if (!stationExists)
            throw new EntityNotFoundException("Station not found on this plan");

        var userIds = await ValidateCoachesAsync(plan, request);

        var existing = await _stationCoachRepository.Query()
            .Where(c => c.StationId == stationId)
            .ToListAsync();

        foreach (var removed in existing.Where(c => !userIds.Contains(c.UserId)))
            _stationCoachRepository.Delete(removed);

        foreach (var added in userIds.Where(id => existing.All(c => c.UserId != id)))
            _stationCoachRepository.Add(new PlanStationCoach { StationId = stationId, UserId = added });

        await _stationCoachRepository.SaveChangesAsync();

        return await ToResolvedDtosAsync(userIds);
    }

    public async Task ResolveNamesAsync(IReadOnlyCollection<PlanCoachDto> coaches)
    {
        if (coaches.Count == 0) return;

        var userIds = coaches.Select(c => c.UserId).Distinct().ToList();
        var profiles = await _userProfileRepository.QueryNoTracking()
            .Where(p => userIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        foreach (var coach in coaches)
        {
            if (!profiles.TryGetValue(coach.UserId, out var profile)) continue;

            coach.FirstName = profile.Name;
            coach.LastName = profile.Surname;
            coach.AvatarUrl = profile.ImageUrl;
            coach.ImageThumbHash = profile.ImageThumbHash;
        }
    }

    private async Task<TrainingPlan> LoadAssignablePlanAsync(Guid planId, Guid userId)
    {
        var plan = await _planRepository.Query()
            .FirstOrDefaultAsync(p => p.Id == planId && !p.IsDeleted)
            ?? throw new EntityNotFoundException("Plan not found");

        // Coaches are event-time people. A template is a shape to reuse at an event that does
        // not exist yet, so there is nobody to assign and no roster to check them against.
        if (plan.PlanType != PlanType.Instance)
            throw new BadRequestException("Only an event's plan can have coaches", ErrorCodeEnum.ValidationError);

        if (!await PlanEditPolicy.CanEditAsync(plan, userId, _eventsGrpcClient))
            throw new ForbiddenException("Only the plan owner or an event admin can assign coaches");

        return plan;
    }

    /// <summary>
    /// The assigned coaches, deduplicated, each confirmed to be on the event. Coaching someone
    /// else's practice is not a thing: a coach has to be at the event to be given a station.
    /// </summary>
    private async Task<List<Guid>> ValidateCoachesAsync(TrainingPlan plan, AssignCoachesDto request)
    {
        var userIds = (request.UserIds ?? []).Distinct().ToList();
        if (userIds.Count == 0) return userIds;

        if (!plan.EventId.HasValue)
            throw new BadRequestException("This plan has no linked event", ErrorCodeEnum.ValidationError);

        foreach (var userId in userIds)
        {
            var (isParticipant, eventExists) = await _eventsGrpcClient.IsEventParticipantAsync(plan.EventId.Value, userId);

            if (!eventExists)
                throw new EntityNotFoundException("The linked event no longer exists");

            if (!isParticipant)
                throw new BadRequestException(
                    $"{userId} is not on this event and cannot be assigned as a coach",
                    ErrorCodeEnum.ValidationError);
        }

        return userIds;
    }

    private async Task<IReadOnlyList<PlanCoachDto>> ToResolvedDtosAsync(IEnumerable<Guid> userIds)
    {
        var dtos = userIds.Select(id => new PlanCoachDto { UserId = id }).ToList();
        await ResolveNamesAsync(dtos);
        return dtos;
    }
}
