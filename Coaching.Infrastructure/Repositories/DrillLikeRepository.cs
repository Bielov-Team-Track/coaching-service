using Coaching.Application.Interfaces.Repositories;
using Coaching.Domain.Models.Drills;
using Coaching.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
using Shared.DataAccess.Repositories;

namespace Coaching.Infrastructure.Repositories;

public class DrillLikeRepository : BaseRepository<DrillLike>, IDrillLikeRepository
{
    public DrillLikeRepository(CoachingDbContext context) : base(context)
    {
    }

    public async Task<DrillLike?> GetByDrillAndUserAsync(Guid drillId, Guid userId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(l => l.DrillId == drillId && l.UserId == userId);
    }

    public async Task<int> GetCountByDrillAsync(Guid drillId)
    {
        return await _dbSet.CountAsync(l => l.DrillId == drillId);
    }

    public async Task<IEnumerable<Guid>> GetUserLikedDrillIdsAsync(Guid userId, IEnumerable<Guid> drillIds)
    {
        var drillIdList = drillIds.ToList();
        return await _dbSet
            .Where(l => l.UserId == userId && drillIdList.Contains(l.DrillId))
            .Select(l => l.DrillId)
            .ToListAsync();
    }
}
