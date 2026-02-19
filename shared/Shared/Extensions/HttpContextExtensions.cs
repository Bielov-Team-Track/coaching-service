using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Shared.Extensions
{
    public static class HttpContextExtensions
    {
        public static bool IsUserLoggedIn(this HttpContext context)
        {
            var identity = context.User.Identity as ClaimsIdentity;

            var userId = identity?.Claims?.FirstOrDefault(x => x.Type == "UserId")?.Value;

            return userId != null;
        }
    }
}