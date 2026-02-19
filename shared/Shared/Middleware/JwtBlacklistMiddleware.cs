using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Shared.Enums;
using Shared.Exceptions;

namespace Shared.Middleware;

public class JwtBlacklistMiddleware
{
    private readonly RequestDelegate _next;

    public JwtBlacklistMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IDistributedCache cache)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var jti = context.User.FindFirst("jti")?.Value;
            if (!string.IsNullOrEmpty(jti))
            {
                var blacklisted = await cache.GetStringAsync($"jwt_blacklist:{jti}");
                if (blacklisted != null)
                {
                    throw new UnauthorizedException(
                        "Token has been revoked",
                        ErrorCodeEnum.TokenInvalid);
                }
            }
        }

        await _next(context);
    }
}
