using Coaching.Domain.Enums;
using Shared.Models;

namespace Coaching.Domain.Models.Templates;

public class TrainingPlan : BaseEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public Guid CreatedByUserId { get; set; }
    public virtual UserProfile? Creator { get; set; }
    public Guid? ClubId { get; set; }
    public TemplateVisibility Visibility { get; set; } = TemplateVisibility.Private;
    public DifficultyLevel Level { get; set; } = DifficultyLevel.Intermediate;
    public PlanType PlanType { get; set; } = PlanType.Template;
    public Guid? EventId { get; set; }       // Cross-service ref to events-service, no FK
    public Guid? SourceTemplateId { get; set; } // No FK - analytics only

    // Denormalized aggregates
    public int TotalDuration { get; set; }
    public int LikeCount { get; set; }
    public int UsageCount { get; set; }

    // Navigation properties
    public virtual ICollection<PlanSection> Sections { get; set; } = new List<PlanSection>();
    public virtual ICollection<PlanItem> Items { get; set; } = new List<PlanItem>();
    public virtual ICollection<PlanLike> Likes { get; set; } = new List<PlanLike>();
    public virtual ICollection<PlanBookmark> Bookmarks { get; set; } = new List<PlanBookmark>();
    public virtual ICollection<PlanComment> Comments { get; set; } = new List<PlanComment>();
}
