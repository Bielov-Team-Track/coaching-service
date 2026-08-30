using Shared.Enums;

namespace Coaching.Application.Interfaces.Services;

/// <summary>
/// gRPC client for guardian-link checks against profiles-service.
/// </summary>
public interface IProfilesGuardianGrpcClient
{
    Task<GuardianAccessResult> CheckGuardianAccessAsync(
        Guid guardianId, Guid minorId, bool checkRemovalNotice = false,
        IEnumerable<ConsentType>? consentTypes = null, CancellationToken ct = default);
}

public record GuardianAccessResult(
    bool HasAccess, bool IsUnderRemovalNotice, GuardianPermission Permissions,
    IReadOnlySet<ConsentType> GrantedConsentTypes);
