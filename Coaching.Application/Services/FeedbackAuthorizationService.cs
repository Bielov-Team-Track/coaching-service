using Coaching.Application.DTOs.Feedback;
using Coaching.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Shared.Exceptions;

namespace Coaching.Application.Services;

/// <summary>
/// Authorization rules for feedback creation:
///
/// RULE 1: Event-linked feedback (EventId is set)
///   - Event type MUST be: TrainingSession, Evaluation, Trial, or Match
///   - Recipient MUST be a participant of the event
///   - IF event has ContextType=Club: user MUST have coaching role (HeadCoach/Owner) in that club
///   - IF event has ContextType=Group/Team/None: user MUST be event admin (Owner/Admin/Organizer)
///
/// RULE 2: Standalone feedback with club (no EventId, ClubId is set)
///   - User MUST have coaching role (HeadCoach/Owner) in that club
///   - Recipient MUST be an active member of that club
///
/// RULE 3: Standalone feedback without club (no EventId, no ClubId)
///   - Currently not supported — ClubId is required for standalone feedback
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
            var isCoach = await clubsClient.IsUserCoachInClubAsync(userId, eventContext.ContextId.Value);
            if (!isCoach)
                return (false, "Only coaches of this club can give feedback on club events", null);

            return (true, null, eventContext.ContextId.Value);
        }

        // Group/Team/None context: user must be event admin
        var isAdmin = await eventsClient.IsEventAdminAsync(eventId, userId);
        if (!isAdmin)
            return (false, "Only event organizers and admins can give feedback on non-club events", null);

        return (true, null, null);
    }

    private async Task<(bool, string?)> ValidateStandaloneWithClubAsync(
        Guid clubId, Guid recipientUserId, Guid userId)
    {
        var isCoach = await clubsClient.IsUserCoachInClubAsync(userId, clubId);
        if (!isCoach)
            return (false, "Only coaches can give standalone feedback to club members");

        var isMember = await clubsClient.IsUserClubMemberAsync(recipientUserId, clubId);
        if (!isMember)
            return (false, "The recipient is not a member of this club");

        return (true, null);
    }
}
