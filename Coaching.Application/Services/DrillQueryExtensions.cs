using Coaching.Application.DTOs.Drills;
using Coaching.Domain.Enums;
using Coaching.Domain.Models.Drills;

namespace Coaching.Application.Services;

/// <summary>
/// Composable query filters for the unified drill list. Kept as pure IQueryable
/// transforms so the filtering logic can be unit-tested in memory and reused.
/// </summary>
public static class DrillQueryExtensions
{
    /// <summary>
    /// Restricts the query to a source scope for an authenticated user.
    /// <list type="bullet">
    /// <item><see cref="DrillScope.All"/>: public drills, the user's own drills, and the contextual club's drills.</item>
    /// <item><see cref="DrillScope.Mine"/>: drills the user created (any visibility).</item>
    /// <item><see cref="DrillScope.Club"/>: drills of the contextual club (none when no club supplied).</item>
    /// <item><see cref="DrillScope.Saved"/>: drills the user bookmarked.</item>
    /// <item><see cref="DrillScope.Liked"/>: drills the user liked.</item>
    /// </list>
    /// </summary>
    public static IQueryable<Drill> ApplyScope(this IQueryable<Drill> query, DrillScope scope, Guid userId, Guid? clubId)
    {
        return scope switch
        {
            DrillScope.Mine => query.Where(d => d.CreatedByUserId == userId),
            DrillScope.Club => clubId.HasValue
                ? query.Where(d => d.ClubId == clubId.Value)
                : query.Where(d => false),
            DrillScope.Saved => query.Where(d => d.Bookmarks.Any(b => b.UserId == userId)),
            DrillScope.Liked => query.Where(d => d.Likes.Any(l => l.UserId == userId)),
            _ => query.Where(d =>
                d.Visibility == DrillVisibility.Public
                || d.CreatedByUserId == userId
                || (clubId.HasValue && d.ClubId == clubId.Value)),
        };
    }

    /// <summary>
    /// Applies the attribute filters (category, intensity, skill, search, equipment).
    /// Multi-select arrays are OR within a dimension and take precedence over the
    /// singular fields; equipment narrows by ALL supplied names.
    /// </summary>
    public static IQueryable<Drill> ApplyAttributeFilters(this IQueryable<Drill> query, DrillFilterRequest filter)
    {
        if (filter.Categories is { Length: > 0 } categories)
            query = query.Where(d => categories.Contains(d.Category));
        else if (filter.Category.HasValue)
            query = query.Where(d => d.Category == filter.Category.Value);

        if (filter.Intensities is { Length: > 0 } intensities)
            query = query.Where(d => intensities.Contains(d.Intensity));
        else if (filter.Intensity.HasValue)
            query = query.Where(d => d.Intensity == filter.Intensity.Value);

        if (filter.Skills is { Length: > 0 } skills)
            query = query.Where(d => d.Skills.Any(s => skills.Contains(s)));
        else if (filter.Skill.HasValue)
            query = query.Where(d => d.Skills.Contains(filter.Skill.Value));

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var searchLower = filter.SearchTerm.ToLower();
            query = query.Where(d =>
                d.Name.ToLower().Contains(searchLower) ||
                (d.Description != null && d.Description.ToLower().Contains(searchLower)));
        }

        if (filter.Equipment is { Length: > 0 } equipment)
        {
            foreach (var equipName in equipment.Select(e => e.ToLower()))
            {
                var name = equipName;
                query = filter.RequiredEquipmentOnly == true
                    ? query.Where(d => d.Equipment.Any(e => !e.IsOptional && e.Name.ToLower().Contains(name)))
                    : query.Where(d => d.Equipment.Any(e => e.Name.ToLower().Contains(name)));
            }
        }

        return query;
    }
}
