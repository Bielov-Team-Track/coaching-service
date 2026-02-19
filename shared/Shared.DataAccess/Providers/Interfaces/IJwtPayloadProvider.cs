using System.Security.Claims;
using Shared.Models.Jwt;

namespace Shared.DataAccess.Providers.Interfaces
{
    public interface IJwtPayloadProvider
    {
        JwtPayloadDto GetJwtPayload(IEnumerable<Claim> claims, string accessToken);
    }
}
