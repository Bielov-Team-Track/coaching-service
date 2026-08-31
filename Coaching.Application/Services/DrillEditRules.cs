using Coaching.Application.Interfaces.Services;
using Coaching.Domain.Models.Drills;
using Shared.Exceptions;

namespace Coaching.Application.Services;

/// <summary>
/// Who is allowed to change a drill. Editing the prose and adding a dial to it are the same
/// act — a dial rewrites the instructions — so they answer to the same rule rather than to two
/// that can drift apart.
/// </summary>
public static class DrillEditRules
{
    public static bool IsCreator(Drill drill, Guid userId) => drill.CreatedByUserId == userId;

    public static async Task EnsureCanManageClubDrillsAsync(Guid clubId, Guid userId, IClubsGrpcClient clubs)
    {
        if (!await clubs.IsUserCoachInClubAsync(userId, clubId))
            throw new ForbiddenException("Only club HeadCoach or above can manage club drills");
    }

    /// <summary>
    /// The whole rule for a drill staying where it is: its creator, and — while it belongs to a
    /// club — only for as long as they still coach there.
    /// </summary>
    public static async Task EnsureCanEditAsync(Drill drill, Guid userId, IClubsGrpcClient clubs)
    {
        if (!IsCreator(drill, userId))
            throw new ForbiddenException("Only the creator can update this drill");

        if (drill.ClubId.HasValue)
            await EnsureCanManageClubDrillsAsync(drill.ClubId.Value, userId, clubs);
    }
}
