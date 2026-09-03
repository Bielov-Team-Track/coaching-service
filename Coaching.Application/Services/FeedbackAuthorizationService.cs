using Coaching.Application.DTOs.Feedback;
using Coaching.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Shared.Enums;
using Shared.Exceptions;

namespace Coaching.Application.Services;

/// <summary>
/// Authorization rules for feedback creation:
///
/// RULE 1: Event-linked feedback (EventId is set)
///   - Event type MUST be: TrainingSession, Evaluation, Trial, or Match
///   - Recipient MUST be a participant of the event
///   - IF event has ContextType=Club: user MUST be able to give feedback in that club
///   - IF event has ContextType=Team/Group: user MUST coach that unit — through a role on the
///     unit, or through a club role that reaches into it — OR be an event admin
///   - IF event has no context: user MUST be event admin (Owner/Admin/Organizer)
///
/// RULE 2: Standalone feedback in a team or group (no EventId, ContextType/ContextId are set)
///   - User MUST coach that unit, by the same test RULE 1 applies to a unit event
///   - Recipient MUST be a member of that unit
///
/// RULE 3: Standalone feedback with club (no EventId, no unit, ClubId is set)
///   - User MUST be able to give feedback in that club
///   - Recipient MUST be an active member of that club
///
/// RULE 4: Standalone feedback without club (no EventId, no unit, no ClubId)
///   - Currently not supported — a club or a unit is required for standalone feedback
///
/// "Able to give feedback" is asked of clubs-service, which owns the role vocabulary. It is
/// wider than club staff: a team's or group's own Coach and AssistantCoach coach the people in
/// front of them, which is the whole reason the unit rules above exist.
/// </summary>
public class FeedbackAuthorizationService(
    IEventsGrpcClient eventsClient,
    IClubsGrpcClient clubsClient,
    ILogger<FeedbackAuthorizationService> logger) : IFeedbackAuthorizationService
{
    private static readonly HashSet<string> AllowedEventTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "TrainingSession", "Evaluation", "Trial", "Match"
    };

    public async Task<Guid?> ValidateCreateAsync(CreateFeedbackDto request, Guid userId)
    {
        var (canCreate, reason, resolvedClubId) = await ValidateInternalAsync(request, userId);
        if (!canCreate)
            throw new ForbiddenException(reason ?? "You are not authorized to create this feedback");
        return resolvedClubId;
    }

    public async Task<bool> CanCreateAsync(CreateFeedbackDto request, Guid userId)
    {
        var (canCreate, reason, _) = await ValidateInternalAsync(request, userId);
        if (!canCreate)
        {
            logger.LogInformation(
                "Feedback creation denied for user {UserId} targeting {RecipientUserId}: {Reason}",
                userId, request.RecipientUserId, reason);
        }
        return canCreate;
    }

    private async Task<(bool CanCreate, string? Reason, Guid? ResolvedClubId)> ValidateInternalAsync(
        CreateFeedbackDto request, Guid userId)
    {
        if (request.RecipientUserId == userId)
            return (false, "You cannot give feedback to yourself", null);

        if (request.EventId.HasValue)
            return await ValidateEventLinkedAsync(request.EventId.Value, request.RecipientUserId, userId);

        if (UnitOf(request.ContextType, request.ContextId) is { } unit)
            return await ValidateStandaloneWithUnitAsync(unit, request.RecipientUserId, userId);

        if (request.ClubId.HasValue)
        {
            var (canCreate, reason) = await ValidateStandaloneWithClubAsync(
                request.ClubId.Value, request.RecipientUserId, userId);
            return (canCreate, reason, canCreate ? request.ClubId.Value : null);
        }

        return (false, "Either eventId or clubId must be provided", null);
    }

    private async Task<(bool, string?, Guid?)> ValidateEventLinkedAsync(
        Guid eventId, Guid recipientUserId, Guid userId)
    {
        // Get event context
        var eventContext = await eventsClient.GetEventContextAsync(eventId);
        if (eventContext == null)
            return (false, "Event not found", null);

        // Check event type
        if (!AllowedEventTypes.Contains(eventContext.EventType))
            return (false,
                $"Feedback cannot be given on {eventContext.EventType} events. Allowed types: TrainingSession, Evaluation, Trial, Match",
                null);

        // Check recipient is a participant
        var (isParticipant, _) = await eventsClient.IsEventParticipantAsync(eventId, recipientUserId);
        if (!isParticipant)
            return (false, "The recipient is not a participant of this event", null);

        // Authorization depends on event context
        if (eventContext.ContextType == "Club" && eventContext.ContextId.HasValue)
        {
            var isCoach = await clubsClient.CanGiveFeedbackInClubAsync(userId, eventContext.ContextId.Value);
            if (!isCoach)
                return (false, "Only coaches of this club can give feedback on club events", null);

            return (true, null, eventContext.ContextId.Value);
        }

        // A team or group event is coached by that unit's coaches, whether or not they happen to
        // have organised this particular session.
        if (UnitOf(eventContext.ContextType, eventContext.ContextId) is { } unit)
        {
            var (coachesUnit, _) = await MayCoachUnitAsync(unit, userId);
            if (coachesUnit)
                return (true, null, null);
        }

        var isAdmin = await eventsClient.IsEventAdminAsync(eventId, userId);
        if (!isAdmin)
            return (false, "Only event organizers and admins can give feedback on non-club events", null);

        return (true, null, null);
    }

    private async Task<(bool, string?, Guid?)> ValidateStandaloneWithUnitAsync(
        UnitContext unit, Guid recipientUserId, Guid userId)
    {
        var (coachesUnit, clubId) = await MayCoachUnitAsync(unit, userId);
        if (!coachesUnit)
            return (false, "Only coaches of this team or group can give feedback to its players", null);

        var isMember = await clubsClient.IsUserUnitMemberAsync(recipientUserId, unit.Type, unit.Id);
        if (!isMember)
            return (false, "The recipient is not a member of this team or group", null);

        return (true, null, clubId);
    }

    private async Task<(bool, string?)> ValidateStandaloneWithClubAsync(
        Guid clubId, Guid recipientUserId, Guid userId)
    {
        var isCoach = await clubsClient.CanGiveFeedbackInClubAsync(userId, clubId);
        if (!isCoach)
            return (false, "Only coaches can give standalone feedback to club members");

        var isMember = await clubsClient.IsUserClubMemberAsync(recipientUserId, clubId);
        if (!isMember)
            return (false, "The recipient is not a member of this club");

        return (true, null);
    }

    /// <summary>
    /// May this user coach the people in one team or group, and which club owns it? A role on the
    /// unit answers first because it needs no second call; a club role that reaches into the unit
    /// is the fallback, and is why club staff need no row on every team they oversee.
    /// </summary>
    private async Task<(bool CoachesUnit, Guid? ClubId)> MayCoachUnitAsync(UnitContext unit, Guid userId)
    {
        var coachesUnit = clubsClient.CanGiveFeedbackInUnitAsync(userId, unit.Type, unit.Id);
        var clubId = await clubsClient.ResolveClubIdAsync(unit.Type, unit.Id);

        if (await coachesUnit)
            return (true, clubId);

        if (clubId is null)
            return (false, null);

        return (await clubsClient.CanGiveFeedbackInClubAsync(userId, clubId.Value), clubId);
    }

    private static UnitContext? UnitOf(ContextType? contextType, Guid? contextId) =>
        contextType is ContextType.Team or ContextType.Group
        && contextId is { } id
        && id != Guid.Empty
            ? new UnitContext(contextType.Value, id)
            : null;

    private static UnitContext? UnitOf(string? contextType, Guid? contextId) =>
        Enum.TryParse<ContextType>(contextType, ignoreCase: true, out var parsed)
            ? UnitOf(parsed, contextId)
            : null;

    private readonly record struct UnitContext(ContextType Type, Guid Id);
}
