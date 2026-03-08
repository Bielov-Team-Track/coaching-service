using Coaching.Domain.Enums;
using Shared.Models;

namespace Coaching.Domain.Models.Drills;

public class Drill : BaseEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }

    public DrillCategory Category { get; set; }
    public DrillIntensity Intensity { get; set; }
    public DrillVisibility Visibility { get; set; }

    // Skills stored as PostgreSQL array
    public DrillSkill[] Skills { get; set; } = [];

    public int? Duration { get; set; } // in minutes
    public int? MinPlayers { get; set; }
    public int? MaxPlayers { get; set; }

    // Instruction data stored as PostgreSQL arrays
    public string[] Instructions { get; set; } = [];
    public string[] CoachingPoints { get; set; } = [];

    // Navigation properties
    public virtual ICollection<DrillEquipment> Equipment { get; set; } = new List<DrillEquipment>();

    // Video preview URL (YouTube, Vimeo, etc.)
    public string? VideoUrl { get; set; }

    // Ownership
    public Guid CreatedByUserId { get; set; }
    public Guid? ClubId { get; set; }

    // Denormalized like count
    public int LikeCount { get; set; }

    // Animation data stored as JSON array
    public string? Animations { get; set; }

    // Navigation properties
    public virtual UserProfile? Creator { get; set; }
    public virtual ICollection<DrillAttachment> Attachments { get; set; } = new List<DrillAttachment>();
    public virtual ICollection<DrillLike> Likes { get; set; } = new List<DrillLike>();
    public virtual ICollection<DrillBookmark> Bookmarks { get; set; } = new List<DrillBookmark>();
    public virtual ICollection<DrillComment> Comments { get; set; } = new List<DrillComment>();

    /// <summary>
    /// Variations of this drill (this drill is the source)
    /// </summary>
    public virtual ICollection<DrillVariation> Variations { get; set; } = new List<DrillVariation>();

    /// <summary>
    /// Drills that have this drill as a variation (this drill is the target)
    /// </summary>
    public virtual ICollection<DrillVariation> VariationOf { get; set; } = new List<DrillVariation>();
}
