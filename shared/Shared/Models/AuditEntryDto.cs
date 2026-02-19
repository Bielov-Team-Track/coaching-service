using Shared.Enums;

namespace Shared.Models;

public class AuditEntryDto
{
    public Guid ActorUserId { get; set; }
    public Guid TargetUserId { get; set; }
    public string ActorRole { get; set; } = "Self";
    public string ActionType { get; set; } = string.Empty;
    public ActionRiskLevel ActionRiskLevel { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public string? AuthorizationSource { get; set; }
}
