namespace Coaching.Application.Interfaces.Services;

/// <summary>
/// Club information returned from clubs-service
/// </summary>
public record ClubInfo(string Name, string? LogoUrl);

/// <summary>
/// gRPC client for fetching club information from clubs-service.
/// </summary>
public interface IClubsGrpcClient
{
    /// <summary>
    /// Get club info for multiple club IDs in a single batch request.
    /// </summary>
    Task<IDictionary<Guid, ClubInfo>> GetClubInfoAsync(IEnumerable<Guid> clubIds);

    /// <summary>
    /// Get a single club info by ID.
    /// </summary>
    Task<ClubInfo?> GetClubInfoAsync(Guid clubId);

    /// <summary>
    /// Get the default skill matrix for a club via gRPC from clubs-service.
    /// Returns null if no default matrix exists.
    /// </summary>
    Task<SkillMatrixInfo?> GetDefaultSkillMatrixAsync(Guid clubId);

    /// <summary>
    /// Get a skill matrix by ID via gRPC from clubs-service.
    /// Returns null if the matrix doesn't exist.
    /// </summary>
    Task<SkillMatrixInfo?> GetSkillMatrixByIdAsync(Guid matrixId);

    /// <summary>
    /// Check if a user has a coaching role (HeadCoach or Owner) at the club level.
    /// Uses the existing CheckUserClubRoles gRPC method.
    ///
    /// Note: ClubRole enum only has HeadCoach (not Coach or AssistantCoach — those exist
    /// only at Team/Group level). Owner is included because club owners should be able
    /// to give feedback to their club members.
    /// </summary>
    Task<bool> IsUserCoachInClubAsync(Guid userId, Guid clubId);

    /// <summary>
    /// Check if a user is an active member of a specific club.
    /// </summary>
    Task<bool> IsUserClubMemberAsync(Guid userId, Guid clubId);
}

/// <summary>
/// Skill matrix information returned from clubs-service gRPC
/// </summary>
public record SkillMatrixInfo(
    Guid MatrixId,
    List<SkillMatrixInfo.SkillInfo> Skills)
{
    public record SkillInfo(
        Guid Id,
        string Name,
        string SkillKey,
        List<BandInfo> Bands);

    public record BandInfo(
        Guid Id,
        int Order,
        string Label,
        decimal MinScore,
        decimal MaxScore);
}
