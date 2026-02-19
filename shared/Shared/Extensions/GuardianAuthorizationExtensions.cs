using Microsoft.AspNetCore.Authorization;

namespace Shared.Extensions
{
    /// <summary>
    /// Extension methods for registering guardian and age-based authorization policies.
    /// </summary>
    public static class GuardianAuthorizationExtensions
    {
        /// <summary>
        /// Adds guardian-related authorization policies to the authorization options.
        /// </summary>
        /// <param name="options">The authorization options to configure.</param>
        public static void AddGuardianPolicies(this AuthorizationOptions options)
        {
            // AdultOnly: Requires user to be an adult (18+)
            options.AddPolicy("AdultOnly", policy =>
                policy.RequireClaim("ageTier", "Adult"));

            // ConsentedMinor: Allows adults OR minors with consent
            // The consent claim is "1" (has consent) or "0" (no consent)
            options.AddPolicy("ConsentedMinor", policy =>
                policy.RequireAssertion(context =>
                    context.User.HasClaim("ageTier", "Adult") ||
                    context.User.HasClaim("consent", "1")));

            // PaymentEligible: Only adults can make payments
            options.AddPolicy("PaymentEligible", policy =>
                policy.RequireAssertion(context =>
                    context.User.HasClaim("ageTier", "Adult")));
        }
    }
}
