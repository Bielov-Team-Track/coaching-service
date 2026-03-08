using Microsoft.Extensions.Caching.Distributed;
using Shared.Services;
using System.Text.Json;

namespace Coaching.Application.Services;

public class CacheOnlyGuardianCacheService : IGuardianCacheService
{
    private readonly IDistributedCache _cache;

    public CacheOnlyGuardianCacheService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<(bool HasAccess, string AuthSource)> HasAccessWithCacheAsync(Guid guardianId, Guid minorId)
    {
        var cacheKey = $"guardian:{guardianId}:{minorId}";
        var cached = await _cache.GetStringAsync(cacheKey);
        return cached != null ? (cached == "1", "Redis") : (false, "CacheMiss");
    }

    public async Task<(bool HasAccess, string AuthSource)> HasAccessFromDbAsync(Guid guardianId, Guid minorId)
    {
        // Non-profiles services don't query the DB directly - use cache only
        return await HasAccessWithCacheAsync(guardianId, minorId);
    }

    public async Task<GuardianRemovalStatus?> GetRemovalNoticeStatusAsync(Guid guardianId, Guid minorId)
    {
        var cacheKey = $"guardian_removal:{guardianId}:{minorId}";
        var cached = await _cache.GetStringAsync(cacheKey);
        if (cached != null)
            return JsonSerializer.Deserialize<GuardianRemovalStatus>(cached);
        return null;
    }

    public async Task InvalidateCacheAsync(Guid guardianId, Guid minorId)
    {
        await _cache.RemoveAsync($"guardian:{guardianId}:{minorId}");
        await _cache.RemoveAsync($"guardian_removal:{guardianId}:{minorId}");
    }
}
