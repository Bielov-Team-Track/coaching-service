using Coaching.Domain.Enums;

namespace Coaching.Application.DTOs.Feedback;

public class FeedbackDto
{
    public Guid Id { get; set; }
    public Guid RecipientUserId { get; set; }
    public Guid CoachUserId { get; set; }
    public Guid? EventId { get; set; }
    public Guid? ClubId { get; set; }
    public Guid? EvaluationId { get; set; }
    public string? Content { get; set; }
    public string? ContentPlainText { get; set; }
    public bool SharedWithPlayer { get; set; }

    // Phase A backward compat: keep Comment in response
    public string? Comment { get; set; }

    public List<ImprovementPointDto> ImprovementPoints { get; set; } = new();
    public PraiseDto? Praise { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class ImprovementPointDto
{
    public Guid Id { get; set; }
    public required string Description { get; set; }
    public int Order { get; set; }
    public List<AttachedDrillReferenceDto> AttachedDrills { get; set; } = new();
    public List<ImprovementPointMediaDto> MediaLinks { get; set; } = new();
}

public class AttachedDrillReferenceDto
{
    public Guid DrillId { get; set; }
}

public class ImprovementPointMediaDto
{
    public Guid Id { get; set; }
    public required string Url { get; set; }
    public FeedbackMediaType Type { get; set; }
    public string? Title { get; set; }
}

public class PraiseDto
{
    public Guid Id { get; set; }
    public required string Message { get; set; }
    public BadgeType? BadgeType { get; set; }
}

public record CreateFeedbackDto
{
    public Guid RecipientUserId { get; set; }
    public Guid? EventId { get; set; }
    public Guid? ClubId { get; set; }
    public string? Content { get; set; }
    public bool SharedWithPlayer { get; set; }
    public List<CreateImprovementPointDto>? ImprovementPoints { get; set; }
    public CreatePraiseDto? Praise { get; set; }

    // Phase A backward compat: accept "comment" from old frontend clients
    public string? Comment { get; set; }
}

public record CreateImprovementPointDto
{
    public required string Description { get; set; }
    public List<Guid>? DrillIds { get; set; }
    public List<CreateImprovementPointMediaDto>? MediaLinks { get; set; }
}

public record CreateImprovementPointMediaDto
{
    public required string Url { get; set; }
    public FeedbackMediaType Type { get; set; }
    public string? Title { get; set; }
}

public record CreatePraiseDto
{
    public required string Message { get; set; }
    public BadgeType? BadgeType { get; set; }
}

public record UpdateFeedbackDto
{
    public string? Content { get; set; }
    public bool? SharedWithPlayer { get; set; }

    // Phase A backward compat
    public string? Comment { get; set; }
}

public record AddImprovementPointDto
{
    public required string Description { get; set; }
    public int? Order { get; set; }
    public List<Guid>? DrillIds { get; set; }
    public List<CreateImprovementPointMediaDto>? MediaLinks { get; set; }
}

public record UpdateImprovementPointDto
{
    public string? Description { get; set; }
}

public record UpdatePraiseDto
{
    public string? Message { get; set; }
    public BadgeType? BadgeType { get; set; }
}

public class FeedbackListResponseDto
{
    public IEnumerable<FeedbackDto> Items { get; set; } = new List<FeedbackDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
