using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared.DataAccess.Providers.Interfaces;
using Shared.Enums;
using Shared.Exceptions;
using Shared.Models.Jwt;

namespace Shared.Microservices.Controllers
{
    [ExcludeFromCodeCoverage]
    [ApiController]
    [Route("v{version:apiVersion}/[controller]")]
    public abstract class BaseApiController : ControllerBase
    {
        private readonly IJwtPayloadProvider _jwtPayloadProvider;

        private JwtPayloadDto? _jwtPayload;

        protected JwtPayloadDto? JwtPayload => _jwtPayload ??= GetJwtPayload();

        protected BaseApiController(IJwtPayloadProvider jwtPayloadProvider)
        {
            _jwtPayloadProvider = jwtPayloadProvider;
        }

        protected void CheckIsUserLoggedIn()
        {
            if (JwtPayload == null || JwtPayload.UserId == Guid.Empty)
            {
                throw new UnauthorizedException("Only logged in users have access to this functionality. Please, login.",
                ErrorCodeEnum.Unauthorized);
            }
        }

        /// <summary>
        /// Returns the minor's UserId if acting as guardian, otherwise the authenticated user's UserId.
        /// Use this for ALL data operations (queries, creates, updates).
        /// </summary>
        protected Guid EffectiveUserId =>
            HttpContext.Items["ActingAsUserId"] as Guid? ?? JwtPayload?.UserId ?? Guid.Empty;

        /// <summary>
        /// Always returns the authenticated user's real UserId, even when acting as guardian.
        /// Use this for audit logging and permission checks.
        /// </summary>
        protected Guid ActualUserId =>
            HttpContext.Items["ActualUserId"] as Guid? ?? JwtPayload?.UserId ?? Guid.Empty;

        /// <summary>
        /// True when the current request is a guardian acting on behalf of a minor.
        /// </summary>
        protected bool IsActingAsGuardian =>
            HttpContext.Items["ActingAsUserId"] != null;

        /// <summary>
        /// Safety check: ensures GuardianContextMiddleware processed any X-Acting-As header.
        /// Call this in controllers that handle guardian write actions.
        /// </summary>
        protected void ValidateGuardianContext()
        {
            if (HttpContext.Items.ContainsKey("ActingAsUserId") &&
                !HttpContext.Items.ContainsKey("GuardianContextProcessed"))
            {
                throw new ForbiddenException("Guardian context present but not validated by middleware");
            }
        }

        private JwtPayloadDto? GetJwtPayload()
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;

            if (identity == null || identity.Claims == null)
            {
                return null;
            }

            var jwtString = GetJwtFromHeader(HttpContext.Request);
            var jwtPayload = _jwtPayloadProvider.GetJwtPayload(identity.Claims, jwtString);

            return jwtPayload;
        }

        private static string GetJwtFromHeader(HttpRequest request)
        {
            if (request.Headers.TryGetValue("Authorization", out var headers))
            {
                return headers[0];
            }

            return string.Empty;
        }
    }
}
