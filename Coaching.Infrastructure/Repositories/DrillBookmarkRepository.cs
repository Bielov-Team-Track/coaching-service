using Coaching.Application.Interfaces.Repositories;
using Coaching.Domain.Models.Drills;
using Coaching.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
using Shared.DataAccess.Repositories;

namespace Coaching.Infrastructure.Repositories;

public class DrillBookmarkRepository : BaseRepository<DrillBookmark>, IDrillBookmarkRepository
{
    public DrillBookmarkRepository(CoachingDbContext context) : base(context)
    {
    }

    public async Task<DrillBookmark?> GetByDrillAndUserAsync(Guid drillId, Guid userId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(b => b.DrillId == drillId && b.UserId == userId);
    }

    public async Task<IEnumerable<DrillBookmark>> GetByUserAsync(Guid userId)
    {
        return await _dbSet
            .Where(b => b.UserId == userId)
            .Include(b => b.Drill)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Guid>> GetUserBookmarkedDrillIdsAsync(Guid userId, IEnumerable<Guid> drillIds)
    {
        var drillIdList = drillIds.ToList();
        return await _dbSet
            .Where(b => b.UserId == userId && drillIdList.Contains(b.DrillId))
            .Select(b => b.DrillId)
            .ToListAsync();
    }

    public async Task<Dictionary<Guid, int>> GetBookmarkCountsAsync(IEnumerable<Guid> drillIds)
    {
        var drillIdList = drillIds.ToList();
        return await _dbSet
            .Where(b => drillIdList.Contains(b.DrillId))
            .GroupBy(b => b.DrillId)
            .Select(g => new { DrillId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.DrillId, x => x.Count);
    }
}
