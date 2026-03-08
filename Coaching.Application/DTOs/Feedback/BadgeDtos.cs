using Coaching.Domain.Enums;

namespace Coaching.Application.DTOs.Feedback;

public class PlayerBadgeDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? PraiseId { get; set; }
    public Guid? EventId { get; set; }
    public BadgeType BadgeType { get; set; }
    public string BadgeName { get; set; } = string.Empty;
    public string BadgeDescription { get; set; } = string.Empty;
    public string BadgeIcon { get; set; } = string.Empty;
    public required string Message { get; set; }
    public Guid AwardedByUserId { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class BadgeStatsDto
{
    public int TotalBadges { get; set; }
    public Dictionary<BadgeType, int> BadgesByType { get; set; } = new();
    public BadgeType? MostCommonBadge { get; set; }
    public PlayerBadgeDto? LatestBadge { get; set; }
}

public record AwardBadgeDto
{
    public Guid UserId { get; set; }
    public Guid? PraiseId { get; set; }
    public Guid? EventId { get; set; }
    public BadgeType BadgeType { get; set; }
    public required string Message { get; set; }
}

public static class BadgeMetadata
{
    public static readonly Dictionary<BadgeType, (string Name, string Description, string Icon)> Badges = new()
    {
        { BadgeType.Star, ("Star Player", "Exceptional performance and standout contribution", "\u2b50") },
        { BadgeType.Improvement, ("Most Improved", "Significant growth and development shown", "\ud83d\udcc8") },
        { BadgeType.Teamwork, ("Team Player", "Outstanding collaboration and support for teammates", "\ud83e\udd1d") },
        { BadgeType.Effort, ("Maximum Effort", "Exceptional dedication and work ethic", "\ud83d\udcaa") },
        { BadgeType.Skill, ("Skilled Performer", "High technical ability demonstrated", "\ud83c\udfaf") },
        { BadgeType.Leadership, ("Leader", "Inspiring and guiding teammates effectively", "\ud83d\udc51") },
        { BadgeType.Consistency, ("Consistent Performer", "Reliable and steady performance", "\ud83d\udd04") },
        { BadgeType.Breakthrough, ("Breakthrough Moment", "Achieved a significant milestone or breakthrough", "\ud83d\ude80") }
    };

    public static (string Name, string Description, string Icon) GetBadgeInfo(BadgeType badgeType)
    {
        return Badges.GetValueOrDefault(badgeType, ("Unknown", "Unknown badge type", "\u2753"));
    }
}
