using Coaching.Application.Interfaces.Services;
using Microsoft.Extensions.Caching.Distributed;
using Shared.Services;

namespace Coaching.Application.Services;

/// <summary>
/// The relationship half of the guardian check, answered from Redis first and profiles-service
/// on a miss. A cache-only answer 403s every pair no other service has looked up yet, so the miss
/// has to ask.
/// </summary>
public class ProfilesGuardianCacheService : IGuardianCacheService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private readonly IDistributedCache _cache;
    private readonly IProfilesGuardianGrpcClient _grpc;

    public ProfilesGuardianCacheService(IDistributedCache cache, IProfilesGuardianGrpcClient grpc)
    {
        _cache = cache;
        _grpc = grpc;
    }

    public async Task<(bool HasAccess, string AuthSource)> HasAccessWithCacheAsync(Guid guardianId, Guid minorId)
    {
        var cacheKey = AccessCacheKey(guardianId, minorId);
        var cached = await _cache.GetStringAsync(cacheKey);
        if (cached != null)
            return (cached == "1", "Redis");

        var result = await _grpc.CheckGuardianAccessAsync(guardianId, minorId);
        await _cache.SetStringAsync(cacheKey, result.HasAccess ? "1" : "0", new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheDuration
        });
        return (result.HasAccess, "ProfilesGrpc");
    }

    public async Task<(bool HasAccess, string AuthSource)> HasAccessFromDbAsync(Guid guardianId, Guid minorId)
    {
        var result = await _grpc.CheckGuardianAccessAsync(guardianId, minorId, checkRemovalNotice: true);
        return (result.HasAccess, "ProfilesGrpc");
    }

    public async Task<GuardianRemovalStatus?> GetRemovalNoticeStatusAsync(Guid guardianId, Guid minorId)
    {
        var result = await _grpc.CheckGuardianAccessAsync(guardianId, minorId, checkRemovalNotice: true);
        return new GuardianRemovalStatus { IsUnderRemovalNotice = result.IsUnderRemovalNotice };
    }

    public async Task InvalidateCacheAsync(Guid guardianId, Guid minorId)
    {
        await _cache.RemoveAsync(AccessCacheKey(guardianId, minorId));
        await _cache.RemoveAsync($"guardian_removal:{guardianId}:{minorId}");
    }

    private static string AccessCacheKey(Guid guardianId, Guid minorId) => $"guardian:{guardianId}:{minorId}";
}
