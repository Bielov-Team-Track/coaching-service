using Coaching.Application.DTOs.Drills;
using Coaching.Domain.Enums;

namespace Coaching.Application.DTOs.Templates;

/// <summary>
/// Author information for a plan
/// </summary>
public class PlanAuthorDto
{
    public Guid Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? AvatarUrl { get; set; }
}

// Response DTOs
public class TrainingPlanDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public Guid CreatedByUserId { get; set; }
    public Guid? ClubId { get; set; }
    public string? ClubName { get; set; }
    public string? ClubLogoUrl { get; set; }
    public PlanAuthorDto? Author { get; set; }
    public TemplateVisibility Visibility { get; set; }
    public DifficultyLevel Level { get; set; }
    public int TotalDuration { get; set; }
    public int LikeCount { get; set; }
    public int UsageCount { get; set; }
    public int CommentCount { get; set; }
    public List<string> Skills { get; set; } = new();
    public int DrillCount { get; set; }
    public int SectionCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class TrainingPlanDetailDto : TrainingPlanDto
{
    public List<PlanSectionDto> Sections { get; set; } = new();
    public List<PlanItemDto> Items { get; set; } = new();
}

public class PlanSectionDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public int Order { get; set; }
    public int Duration { get; set; }
    public List<PlanItemDto> Items { get; set; } = new();
}

public class PlanItemDto
{
    public Guid Id { get; set; }
    public Guid DrillId { get; set; }
    public Guid? SectionId { get; set; }
    public int Order { get; set; }
    public int Duration { get; set; }
    public string? Notes { get; set; }
    public DrillDto? Drill { get; set; }
}

// Request DTOs
public record CreatePlanDto(
    string Name,
    string? Description,
    Guid? ClubId,
    TemplateVisibility Visibility = TemplateVisibility.Private,
    DifficultyLevel Level = DifficultyLevel.Intermediate,
    List<CreatePlanSectionDto>? Sections = null,
    List<CreatePlanItemDto>? Items = null
);

public record UpdatePlanDto(
    string? Name,
    string? Description,
    Guid? ClubId,
    TemplateVisibility? Visibility,
    DifficultyLevel? Level,
    List<CreatePlanSectionDto>? Sections = null,
    List<CreatePlanItemDto>? Items = null
);

public record CreatePlanSectionDto(
    string Name,
    int Order,
    Guid? Id = null
);

public record UpdatePlanSectionDto(
    string? Name,
    int? Order
);

public record CreatePlanItemDto(
    Guid DrillId,
    Guid? SectionId,
    int Duration,
    string? Notes,
    int? Order = null
);

public record UpdatePlanItemDto(
    Guid? SectionId,
    int? Duration,
    string? Notes
);

public record ReorderPlanItemsDto(
    List<Guid> ItemIds
);

// List/Filter DTOs
public class PlanFilterRequest
{
    public string? SearchTerm { get; set; }
    public int? MinDuration { get; set; }
    public int? MaxDuration { get; set; }
    public List<string>? Skills { get; set; }
    public DifficultyLevel? Level { get; set; }
    public string SortBy { get; set; } = "newest";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class PlanListResponseDto
{
    public IEnumerable<TrainingPlanDto> Items { get; set; } = new List<TrainingPlanDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

// Save from Event DTO
public record SaveAsTemplateDto(
    string Name,
    string? Description,
    Guid? ClubId,
    TemplateVisibility Visibility = TemplateVisibility.Private,
    DifficultyLevel Level = DifficultyLevel.Intermediate
);

// Event Plan DTOs
public record CreateEventPlanDto(
    string? Name,
    string? Description,
    Guid? SourceTemplateId,  // If copying from template
    List<CreatePlanSectionDto>? Sections = null,
    List<CreatePlanItemDto>? Items = null
);

public record PromotePlanDto(
    string? Name,           // Optional override name for the new template
    Guid? ClubId            // Optional club to assign the template to
);
