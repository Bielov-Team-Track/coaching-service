using Coaching.Application.Interfaces.Services;
using Coaching.Domain.Enums;
using Coaching.Domain.Models.Templates;

namespace Coaching.Application.Services;

/// <summary>
/// Who may change a plan. One rule in one place because two services ask it: the plan service
/// for edits, and the coach service for distributing coaches across the practice.
/// </summary>
internal static class PlanEditPolicy
{
    /// <summary>
    /// The owner always may. For an event's plan the event's admins may too — the lead coach of
    /// the event has to be able to shape the practice and hand out the stations, and they are
    /// often not whoever happened to create the plan. A template has no event, so it stays with
    /// its owner.
    /// </summary>
    public static async Task<bool> CanEditAsync(TrainingPlan plan, Guid userId, IEventsGrpcClient eventsClient)
    {
        if (plan.CreatedByUserId == userId)
            return true;

        if (plan.PlanType == PlanType.Instance && plan.EventId.HasValue)
            return await eventsClient.IsEventAdminAsync(plan.EventId.Value, userId);

        return false;
    }
}
