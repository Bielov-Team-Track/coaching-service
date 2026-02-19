using Shared.Enums;
using Microsoft.AspNetCore.Http;

namespace Shared.Services;

public interface IActionRiskClassifier
{
    ActionRiskLevel GetRiskLevel(HttpRequest request);
}
