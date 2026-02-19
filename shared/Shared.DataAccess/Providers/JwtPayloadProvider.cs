using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Shared.DataAccess.Providers.Interfaces;
using Shared.Models.Jwt;

namespace Shared.DataAccess.Providers
{
    public class JwtPayloadProvider : IJwtPayloadProvider
    {
        public JwtPayloadDto GetJwtPayload(IEnumerable<Claim> claims, string accessToken)
        {
            Guid userId;

            try
            {
                string? userIdClaim = null;

                // Try different claim types that might contain the user ID
                var claimTypes = new[]
                {
                    JwtRegisteredClaimNames.NameId,  // "nameid" - what auth service uses
                    ClaimTypes.NameIdentifier,       // Standard .NET claim type
                    "UserId",                        // Custom claim type
                    "sub"                            // Subject claim (JWT standard)
                };

                foreach (var claimType in claimTypes)
                {
                    userIdClaim = claims.FirstOrDefault(x => x.Type == claimType)?.Value;
                    if (!string.IsNullOrEmpty(userIdClaim))
                    {
                        break;
                    }
                }

                if (userIdClaim == null)
                {
                    userIdClaim = Guid.Empty.ToString();
                }

                userId = Guid.Parse(userIdClaim);
            }
            catch
            {
                throw new ArgumentException("ApiToken has wrong JWT format");
            }

            accessToken = accessToken.Replace("Bearer ", "");

            return new JwtPayloadDto
            {
                AccessToken = accessToken,
                UserId = userId,
                Email = claims.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value ?? string.Empty
            };
        }
    }
}
