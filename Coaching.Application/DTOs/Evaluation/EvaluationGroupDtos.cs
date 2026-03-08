namespace Coaching.Application.DTOs.Evaluation;

public class EvaluationGroupDto
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? EvaluatorUserId { get; set; }
    public string? EvaluatorName { get; set; }
    public int Order { get; set; }
    public List<GroupPlayerDto> Players { get; set; } = new();
}

public class GroupPlayerDto
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public string? PlayerName { get; set; }
    public string? AvatarUrl { get; set; }
}

public record CreateGroupDto
{
    public required string Name { get; set; }
    public Guid? EvaluatorUserId { get; set; }
    public List<Guid>? PlayerIds { get; set; }
}

public record UpdateGroupDto
{
    public string? Name { get; set; }
    public Guid? EvaluatorUserId { get; set; }
}

public record AutoSplitGroupsDto
{
    public int NumberOfGroups { get; set; }
}

public record AssignPlayerToGroupDto
{
    public Guid PlayerId { get; set; }
}

public record MovePlayerDto
{
    public Guid PlayerId { get; set; }
    public Guid TargetGroupId { get; set; }
}
