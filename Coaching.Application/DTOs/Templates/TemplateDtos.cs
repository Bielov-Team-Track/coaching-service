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

/// <summary>
/// A coach assigned to an event's plan or to one of its stations. Named fields match
/// <see cref="PlanAuthorDto"/> and <see cref="UserProfileDto"/> so a client has one shape
/// for "a person" across this API; the name is resolved from the local profile replica and
/// stays null for a coach whose profile has not replicated yet.
/// </summary>
public class PlanCoachDto
{
    public Guid UserId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? ImageThumbHash { get; set; }
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

    /// <summary>Total minus breaks and meetings — the time actually spent coaching.</summary>
    public int CoachedDuration { get; set; }

    public int LikeCount { get; set; }
    public int UsageCount { get; set; }

    /// <summary>Null for an anonymous read; the viewer's own state otherwise.</summary>
    public bool? IsLiked { get; set; }
    public bool? IsBookmarked { get; set; }
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

    /// <summary>Event plans only: the coaches working the practice. Empty on a template.</summary>
    public List<PlanCoachDto> Coaches { get; set; } = new();
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
    public ItemKind Kind { get; set; }
    public Guid? DrillId { get; set; }
    public string? Title { get; set; }

    public Guid? SectionId { get; set; }
    public int Order { get; set; }
    public int Duration { get; set; }
    public string? Notes { get; set; }
    public DrillDto? Drill { get; set; }

    /// <summary>Whether this row is coaching time. Breaks and meetings are not.</summary>
    public bool IsCoached { get; set; }

    /// <summary>Stations only: the length the coach asked for, which the groups may exceed.</summary>
    public int? PlannedDuration { get; set; }

    /// <summary>Stations only: the groups running side by side inside this row.</summary>
    public List<PlanStationDto> Stations { get; set; } = new();

    /// <summary>
    /// What this use of the drill decided its dials should say, by dial name. Empty for a row
    /// with no drill, or one whose drill has no dials. A name with no matching dial is kept
    /// rather than dropped — a dial removed from the drill leaves its answers behind harmlessly.
    /// </summary>
    public Dictionary<string, string> DialValues { get; set; } = new();
}

public class PlanStationDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public int Order { get; set; }
    public List<PlanStationItemDto> Items { get; set; } = new();

    /// <summary>The coaches running this group.</summary>
    public List<PlanCoachDto> Coaches { get; set; } = new();
}

public class PlanStationItemDto
{
    public Guid Id { get; set; }
    public ItemKind Kind { get; set; }
    public Guid? DrillId { get; set; }
    public string? Title { get; set; }
    public int Order { get; set; }
    public int Duration { get; set; }
    public string? Notes { get; set; }
    public DrillDto? Drill { get; set; }

    /// <summary>Whether this row is coaching time. Breaks are not.</summary>
    public bool IsCoached { get; set; }

    /// <summary>What this use of the drill decided its dials should say, by dial name.</summary>
    public Dictionary<string, string> DialValues { get; set; } = new();
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

/// <param name="Id">The section this entry is, when it already exists — see <see cref="CreatePlanItemDto.Id"/>.</param>
public record CreatePlanSectionDto(
    string Name,
    int Order,
    Guid? Id = null
);

public record UpdatePlanSectionDto(
    string? Name,
    int? Order
);

/// <param name="Id">
/// The row this entry is, when it already exists. Absent for a row being added, in which case the
/// server assigns one — or honours the id the client minted for it. Sending it back is what keeps
/// a save from destroying everything keyed to the row: its station coaches, its floor placement,
/// its progress in a run that is under way.
/// </param>
/// <param name="DialValues">
/// What this use of the drill sets its dials to, by dial name. It rides the item because the rows
/// are keyed to it by id alone — values sent any other way would have nothing to attach to.
/// </param>
public record CreatePlanItemDto(
    Guid? DrillId,
    Guid? SectionId,
    int Duration,
    string? Notes,
    int? Order = null,
    ItemKind Kind = ItemKind.Drill,
    string? Title = null,
    int? PlannedDuration = null,
    List<CreatePlanStationDto>? Stations = null,
    Dictionary<string, string>? DialValues = null,
    Guid? Id = null
);

/// <param name="Id">The group this entry is, when it already exists — see <see cref="CreatePlanItemDto.Id"/>.</param>
public record CreatePlanStationDto(
    string Name,
    int Order,
    List<CreatePlanStationItemDto>? Items = null,
    Guid? Id = null
);

/// <param name="Id">The row this entry is, when it already exists — see <see cref="CreatePlanItemDto.Id"/>.</param>
public record CreatePlanStationItemDto(
    Guid? DrillId,
    int Duration,
    string? Notes,
    int Order,
    ItemKind Kind = ItemKind.Drill,
    string? Title = null,
    Dictionary<string, string>? DialValues = null,
    Guid? Id = null
);

public record UpdatePlanItemDto(
    Guid? SectionId,
    int? Duration,
    string? Notes,
    ItemKind? Kind = null,
    string? Title = null
);

public record ReorderPlanItemsDto(
    List<Guid> ItemIds
);

/// <summary>
/// Replaces a coach set outright: whoever is named here is assigned afterwards, and whoever
/// is not is unassigned. An empty list clears the set.
/// </summary>
public record AssignCoachesDto(
    List<Guid>? UserIds
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
