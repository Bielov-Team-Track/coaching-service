using Coaching.Application.Interfaces.Services;
using Shared.Contracts.Grpc;
using Shared.Enums;

namespace Coaching.Infrastructure.Services;

public class ProfilesGuardianGrpcClient : IProfilesGuardianGrpcClient
{
    private readonly UserProfileService.UserProfileServiceClient _client;

    public ProfilesGuardianGrpcClient(UserProfileService.UserProfileServiceClient client)
    {
        _client = client;
    }

    public async Task<GuardianAccessResult> CheckGuardianAccessAsync(
        Guid guardianId, Guid minorId, bool checkRemovalNotice = false,
        IEnumerable<ConsentType>? consentTypes = null, CancellationToken ct = default)
    {
        var request = new CheckGuardianAccessRequest
        {
            GuardianId = guardianId.ToString(),
            MinorId = minorId.ToString(),
            CheckRemovalNotice = checkRemovalNotice,
        };

        if (consentTypes is not null)
        {
            foreach (var consentType in consentTypes)
                request.ConsentTypes.Add(consentType.ToString());
        }

        var resp = await _client.CheckGuardianAccessAsync(request, cancellationToken: ct);

        var grantedConsentTypes = resp.GrantedConsentTypes
            .Select(name => Enum.TryParse<ConsentType>(name, out var parsed) ? (ConsentType?)parsed : null)
            .Where(t => t.HasValue)
            .Select(t => t!.Value)
            .ToHashSet();

        return new GuardianAccessResult(
            resp.HasAccess,
            resp.IsUnderRemovalNotice,
            (GuardianPermission)resp.Permissions,
            grantedConsentTypes);
    }
}
