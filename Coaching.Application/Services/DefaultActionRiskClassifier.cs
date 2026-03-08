using Microsoft.AspNetCore.Http;
using Shared.Enums;
using Shared.Services;

namespace Coaching.Application.Services;

public class DefaultActionRiskClassifier : IActionRiskClassifier
{
    public ActionRiskLevel GetRiskLevel(HttpRequest request)
    {
        var method = request.Method.ToUpperInvariant();
        var path = request.Path.Value?.ToLowerInvariant() ?? string.Empty;

        if (path.Contains("/payments") || path.Contains("/checkout"))
            return ActionRiskLevel.Critical;
        if (method == "DELETE")
            return ActionRiskLevel.High;
        if (method is "PUT" or "PATCH")
            return ActionRiskLevel.Medium;
        if (method == "POST")
            return ActionRiskLevel.Medium;
        if (method == "GET")
            return ActionRiskLevel.Low;
        return ActionRiskLevel.Medium;
    }
}
