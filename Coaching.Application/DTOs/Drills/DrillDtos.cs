using Coaching.Domain.Enums;
using System.Text.Json.Serialization;

namespace Coaching.Application.DTOs.Drills;

// Animation Types (stored as JSON in database)

/// <summary>
/// Position of a player in an animation frame
/// </summary>
public class PlayerPositionDto
{
    public required string Id { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public required string Color { get; set; }
    public string? Label { get; set; }
    public int? FirstFrameIndex { get; set; }
    public string? Note { get; set; }
}

/// <summary>
/// Equipment item in an animation frame
/// </summary>
public class EquipmentItemDto
{
    public required string Id { get; set; }
    public required string Type { get; set; } // cone, target, ball, hoop, ladder, hurdle, antenna
    public double X { get; set; }
    public double Y { get; set; }
    public double? Rotation { get; set; }
    public int? FirstFrameIndex { get; set; }
    public string? Note { get; set; }
    public string? Label { get; set; }
}

/// <summary>
/// Ball position in an animation frame
/// </summary>
public class BallPositionDto
{
    public double X { get; set; }
    public double Y { get; set; }
}

/// <summary>
/// A single keyframe in a drill animation
/// </summary>
public class AnimationKeyframeDto
{
    public required string Id { get; set; }
    public List<PlayerPositionDto> Players { get; set; } = [];
    public required BallPositionDto Ball { get; set; }
    public List<EquipmentItemDto>? Equipment { get; set; }
}

/// <summary>
/// A complete drill animation with keyframes and speed setting
/// </summary>
public class DrillAnimationDto
{
    public string? Name { get; set; }
    public List<AnimationKeyframeDto> Keyframes { get; set; } = [];
    public int Speed { get; set; } // milliseconds per frame transition
}

/// <summary>
/// Request to update a drill's animations
/// </summary>
public record UpdateDrillAnimationsDto(
    List<DrillAnimationDto> Animations
);

/// <summary>
/// Author information for a drill
/// </summary>
public class DrillAuthorDto
{
    public Guid Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? AvatarUrl { get; set; }
}

/// <summary>
/// Equipment item for a drill
/// </summary>
public class DrillEquipmentDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public bool IsOptional { get; set; }
    public int Order { get; set; }
}

/// <summary>
/// Input for creating/updating equipment on a drill
/// </summary>
public record DrillEquipmentInput(
    string Name,
    bool IsOptional = false
);

/// <summary>
/// Represents a variation link to another drill
/// </summary>
public class DrillVariationDto
{
    public Guid Id { get; set; }
    public Guid DrillId { get; set; }
    public required string DrillName { get; set; }
    public DrillCategory DrillCategory { get; set; }
    public DrillIntensity DrillIntensity { get; set; }
    public string? Note { get; set; }
    public int Order { get; set; }
}

public class DrillDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }

    public DrillCategory Category { get; set; }
    public DrillIntensity Intensity { get; set; }
    public DrillVisibility Visibility { get; set; }
    public DrillSkill[] Skills { get; set; } = [];

    public int? Duration { get; set; }
    public int? MinPlayers { get; set; }
    public int? MaxPlayers { get; set; }

    public string[] Instructions { get; set; } = [];
    public string[] CoachingPoints { get; set; } = [];
    public ICollection<DrillEquipmentDto> Equipment { get; set; } = new List<DrillEquipmentDto>();

    public string? VideoUrl { get; set; }

    public Guid CreatedByUserId { get; set; }
    public Guid? ClubId { get; set; }
    public string? ClubName { get; set; }
    public string? ClubLogoUrl { get; set; }
    public DrillAuthorDto? Author { get; set; }

    public int LikeCount { get; set; }
    public int BookmarkCount { get; set; }

    /// <summary>
    /// Whether the current user has liked this drill (null if not authenticated)
    /// </summary>
    public bool? IsLiked { get; set; }

    /// <summary>
    /// Whether the current user has bookmarked this drill (null if not authenticated)
    /// </summary>
    public bool? IsBookmarked { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<DrillAttachmentDto> Attachments { get; set; } = new List<DrillAttachmentDto>();
    public ICollection<DrillVariationDto> Variations { get; set; } = new List<DrillVariationDto>();

    public List<DrillAnimationDto> Animations { get; set; } = [];
}

/// <summary>
/// Input for adding a variation to a drill
/// </summary>
public record CreateDrillVariationInput(
    Guid DrillId,
    string? Note
);

public record CreateDrillDto(
    string Name,
    string? Description,
    DrillCategory Category,
    DrillIntensity Intensity,
    DrillVisibility Visibility,
    DrillSkill[] Skills,
    int? Duration,
    int? MinPlayers,
    int? MaxPlayers,
    string[] Instructions,
    string[] CoachingPoints,
    CreateDrillVariationInput[] Variations,
    DrillEquipmentInput[] Equipment,
    string? VideoUrl,
    Guid? ClubId
);

public record UpdateDrillDto(
    Guid Id,
    string Name,
    string? Description,
    DrillCategory Category,
    DrillIntensity Intensity,
    DrillVisibility Visibility,
    DrillSkill[] Skills,
    int? Duration,
    int? MinPlayers,
    int? MaxPlayers,
    string[] Instructions,
    string[] CoachingPoints,
    CreateDrillVariationInput[] Variations,
    DrillEquipmentInput[] Equipment,
    string? VideoUrl,
    Guid? ClubId
);

public class DrillFilterRequest
{
    public DrillCategory? Category { get; set; }
    public DrillIntensity? Intensity { get; set; }
    public DrillSkill? Skill { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? ClubId { get; set; }
    public string? SearchTerm { get; set; }
    public DrillVisibility? Visibility { get; set; }

    // Equipment filter
    public string[]? Equipment { get; set; } // Filter by equipment names (any match)
    public bool? RequiredEquipmentOnly { get; set; } // Only filter by required equipment (exclude optional)

    // Sorting
    public string? SortBy { get; set; } // likeCount, name, createdAt, duration
    public string? SortOrder { get; set; } // asc, desc

    // Pagination
    public int Page { get; set; } = 1;
    public int Limit { get; set; } = 20;
}
