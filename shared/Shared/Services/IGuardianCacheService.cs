namespace Shared.Services;

public interface IGuardianCacheService
{
    Task<(bool HasAccess, string AuthSource)> HasAccessWithCacheAsync(Guid guardianId, Guid minorId);
    Task<(bool HasAccess, string AuthSource)> HasAccessFromDbAsync(Guid guardianId, Guid minorId);
    Task<GuardianRemovalStatus?> GetRemovalNoticeStatusAsync(Guid guardianId, Guid minorId);
    Task InvalidateCacheAsync(Guid guardianId, Guid minorId);
}

public class GuardianRemovalStatus
{
    public bool IsUnderRemovalNotice { get; set; }
    public DateTime? RemovalNoticeStartedAt { get; set; }
}
