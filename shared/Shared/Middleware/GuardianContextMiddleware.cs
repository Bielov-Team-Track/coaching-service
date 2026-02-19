using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Shared.Enums;
using Shared.Exceptions;
using Shared.Services;

namespace Shared.Middleware;

public class GuardianContextMiddleware
{
    private readonly RequestDelegate _next;

    public GuardianContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IGuardianCacheService guardianService,
        IActionRiskClassifier actionClassifier)
    {
        if (context.Request.Headers.TryGetValue("X-Acting-As", out var rawValue))
        {
            // Step 1: Require authenticated user
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)
                ?? context.User.FindFirst("nameid");

            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var jwtUserId))
                throw new UnauthorizedException(
                    "Authentication required",
                    ErrorCodeEnum.Unauthorized);

            // Step 2: Strict Guid format validation
            if (!Guid.TryParse(rawValue.ToString(), out var minorId))
                throw new BadRequestException(
                    "X-Acting-As must be a valid GUID",
                    ErrorCodeEnum.ActingAsValidationFailed);

            // Step 3: Cannot act as yourself
            if (minorId == jwtUserId)
                throw new BadRequestException(
                    "Cannot act as yourself",
                    ErrorCodeEnum.ActingAsValidationFailed);

            // Step 4: Fixed-delay-then-process (constant-time to prevent timing-based enumeration)
            await Task.Delay(100);

            // Step 5: Validate guardian relationship
            var riskLevel = actionClassifier.GetRiskLevel(context.Request);

            var (hasAccess, authSource) = riskLevel >= ActionRiskLevel.High
                ? await guardianService.HasAccessFromDbAsync(jwtUserId, minorId)
                : await guardianService.HasAccessWithCacheAsync(jwtUserId, minorId);

            if (!hasAccess)
                throw new ForbiddenException("Access denied");

            // Step 6: Check removal notice — reject writes during removal period
            var removalStatus = await guardianService.GetRemovalNoticeStatusAsync(jwtUserId, minorId);
            if (removalStatus?.IsUnderRemovalNotice == true && IsWriteAction(context.Request))
                throw new ForbiddenException("Access denied");

            // Step 7: Set validated context — controllers read ONLY from here
            context.Items["ActingAsUserId"] = minorId;
            context.Items["ActualUserId"] = jwtUserId;
            context.Items["AuthorizationSource"] = authSource;
            context.Items["GuardianContextProcessed"] = true;
        }

        await _next(context);
    }

    private static bool IsWriteAction(HttpRequest request)
    {
        return request.Method is "POST" or "PUT" or "PATCH" or "DELETE";
    }
}
