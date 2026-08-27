using Coaching.Application.Interfaces.Services;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Shared.Enums;
using Shared.Services;

namespace Coaching.Infrastructure.Services;

/// <summary>
/// The shared authorizer's view of profiles-service, over the gRPC client coaching owns.
/// </summary>
public class ProfilesGuardianAccessSource(
    IProfilesGuardianGrpcClient grpc,
    ILogger<ProfilesGuardianAccessSource> logger) : IGuardianAccessSource
{
    public async Task<GuardianAccessSnapshot?> CheckAsync(
        Guid guardianUserId,
        Guid subjectUserId,
        IReadOnlyCollection<ConsentType>? requiredConsents,
        CancellationToken ct = default)
    {
        try
        {
            var result = await grpc.CheckGuardianAccessAsync(
                guardianUserId, subjectUserId,
                checkRemovalNotice: true,
                consentTypes: requiredConsents,
                ct: ct);

            return new GuardianAccessSnapshot(
                result.HasAccess,
                result.IsUnderRemovalNotice,
                result.Permissions,
                result.GrantedConsentTypes);
        }
        catch (RpcException ex)
        {
            // Null is "could not ask", which the authorizer fails closed on. Returning a
            // no-access snapshot instead would be indistinguishable from a real refusal.
            logger.LogWarning(ex,
                "Guardian access check failed for {GuardianUserId} acting for {SubjectUserId}",
                guardianUserId, subjectUserId);
            return null;
        }
    }
}
