using Coaching.Application.Interfaces.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Grpc;

namespace Coaching.Infrastructure.Services;

/// <summary>
/// gRPC client for clubs-service with in-memory caching for club info.
/// Club info is cached for 5 minutes since it rarely changes.
/// </summary>
public class ClubsGrpcClient : IClubsGrpcClient
{
    private readonly ClubsInternalService.ClubsInternalServiceClient _grpcClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ClubsGrpcClient> _logger;

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private const string CacheKeyPrefix = "club_info_";

    public ClubsGrpcClient(
        ClubsInternalService.ClubsInternalServiceClient grpcClient,
        IMemoryCache cache,
        ILogger<ClubsGrpcClient> logger)
    {
        _grpcClient = grpcClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IDictionary<Guid, ClubInfo>> GetClubInfoAsync(IEnumerable<Guid> clubIds)
    {
        var uniqueIds = clubIds.Where(id => id != Guid.Empty).Distinct().ToList();
        if (uniqueIds.Count == 0)
            return new Dictionary<Guid, ClubInfo>();

        var result = new Dictionary<Guid, ClubInfo>();
        var uncachedIds = new List<Guid>();

        // Check cache first
        foreach (var clubId in uniqueIds)
        {
            var cacheKey = $"{CacheKeyPrefix}{clubId}";
            if (_cache.TryGetValue(cacheKey, out ClubInfo? cachedInfo) && cachedInfo != null)
            {
                result[clubId] = cachedInfo;
            }
            else
            {
                uncachedIds.Add(clubId);
            }
        }

        // Fetch uncached items from gRPC
        if (uncachedIds.Count > 0)
        {
            _logger.LogDebug("Skipping club info fetch - GetClubNamesAsync not yet implemented in clubs-service gRPC");
        }

        return result;
    }

    public async Task<ClubInfo?> GetClubInfoAsync(Guid clubId)
    {
        if (clubId == Guid.Empty)
            return null;

        var cacheKey = $"{CacheKeyPrefix}{clubId}";

        if (_cache.TryGetValue(cacheKey, out ClubInfo? cachedInfo))
            return cachedInfo;

        var result = await GetClubInfoAsync([clubId]);
        return result.TryGetValue(clubId, out var info) ? info : null;
    }

    public async Task<SkillMatrixInfo?> GetDefaultSkillMatrixAsync(Guid clubId)
    {
        if (clubId == Guid.Empty)
            return null;

        try
        {
            var response = await _grpcClient.GetSkillMatrixAsync(new GetSkillMatrixRequest
            {
                ClubId = clubId.ToString()
            });

            if (!response.Found)
                return null;

            return MapToSkillMatrixInfo(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get skill matrix from clubs-service for club {ClubId}", clubId);
            return null;
        }
    }

    public async Task<SkillMatrixInfo?> GetSkillMatrixByIdAsync(Guid matrixId)
    {
        // The current gRPC proto only supports GetSkillMatrix by clubId.
        // For now, return null - this can be updated when the proto is extended.
        _logger.LogWarning("GetSkillMatrixByIdAsync not yet supported via gRPC - matrix {MatrixId}", matrixId);
        return null;
    }

    // Club-level roles that grant coaching/feedback authority.
    private static readonly HashSet<string> CoachRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "HeadCoach", "Owner"
    };

    private record ClubRolesResult(bool IsMember, bool IsCoach);

    private async Task<ClubRolesResult> GetClubRolesAsync(Guid userId, Guid clubId)
    {
        var cacheKey = $"club_roles_{clubId}_{userId}";

        if (_cache.TryGetValue(cacheKey, out ClubRolesResult? cached))
            return cached!;

        try
        {
            var response = await _grpcClient.CheckUserClubRolesAsync(new CheckUserClubRolesRequest
            {
                UserId = userId.ToString(),
                ClubId = clubId.ToString()
            });

            var result = new ClubRolesResult(
                response.IsMember,
                response.IsMember && response.Roles.Any(r => CoachRoles.Contains(r)));

            _cache.Set(cacheKey, result, CacheDuration);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check club roles via gRPC for user {UserId} in club {ClubId}", userId, clubId);
            throw;
        }
    }

    public async Task<bool> IsUserCoachInClubAsync(Guid userId, Guid clubId)
    {
        var roles = await GetClubRolesAsync(userId, clubId);
        return roles.IsCoach;
    }

    public async Task<bool> IsUserClubMemberAsync(Guid userId, Guid clubId)
    {
        var roles = await GetClubRolesAsync(userId, clubId);
        return roles.IsMember;
    }

    private static SkillMatrixInfo MapToSkillMatrixInfo(GetSkillMatrixResponse response)
    {
        return new SkillMatrixInfo(
            Guid.Parse(response.MatrixId),
            response.Skills.Select(s => new SkillMatrixInfo.SkillInfo(
                Guid.Parse(s.SkillId),
                s.Name,
                s.SkillKey,
                s.Bands.Select(b => new SkillMatrixInfo.BandInfo(
                    Guid.Parse(b.Id),
                    b.Order,
                    b.Label,
                    (decimal)b.MinScore,
                    (decimal)b.MaxScore
                )).ToList()
            )).ToList()
        );
    }
}
