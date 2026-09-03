using Coaching.Application.DTOs.Feedback;

namespace Coaching.Application.Interfaces.Services;

/// <summary>
/// Validates authorization for feedback operations.
/// Throws ForbiddenException with descriptive message if not authorized.
/// </summary>
public interface IFeedbackAuthorizationService
{
    /// <summary>
    /// Validates the user can create feedback for the given request.
    /// Returns the resolved ClubId — from the event's context when the feedback is event-linked,
    /// from the owning club when a team or group is named, and from the request when a club is
    /// all it carries — so FeedbackService can use it without making a duplicate gRPC call.
    /// </summary>
    Task<Guid?> ValidateCreateAsync(CreateFeedbackDto request, Guid userId);

    /// <summary>
    /// Checks if the user can create feedback (non-throwing version for the can-create endpoint).
    /// Returns canCreate boolean only. Reasons are logged server-side.
    /// </summary>
    Task<bool> CanCreateAsync(CreateFeedbackDto request, Guid userId);
}
