using Coaching.Application.Interfaces.Repositories;
using Coaching.Domain.Models.Drills;
using Coaching.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
using Shared.DataAccess.Repositories;

namespace Coaching.Infrastructure.Repositories;

public class DrillCommentRepository : BaseRepository<DrillComment>, IDrillCommentRepository
{
    public DrillCommentRepository(CoachingDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<DrillComment>> GetByDrillWithCursorAsync(Guid drillId, Guid? cursor, int limit)
    {
        var query = _dbSet
            .Where(c => c.DrillId == drillId && c.ParentCommentId == null)
            .Include(c => c.User)
            .Include(c => c.Replies.Where(r => !r.IsDeleted))
                .ThenInclude(r => r.User)
            .OrderByDescending(c => c.CreatedAt);

        if (cursor.HasValue)
        {
            var cursorComment = await _dbSet.FindAsync(cursor.Value);
            if (cursorComment != null)
            {
                query = (IOrderedQueryable<DrillComment>)query
                    .Where(c => c.CreatedAt < cursorComment.CreatedAt ||
                               (c.CreatedAt == cursorComment.CreatedAt && c.Id != cursor.Value));
            }
        }

        return await query.Take(limit + 1).ToListAsync();
    }

    public async Task<DrillComment?> GetByIdWithDetailsAsync(Guid id)
    {
        return await _dbSet
            .Include(c => c.User)
            .Include(c => c.Replies.Where(r => !r.IsDeleted))
                .ThenInclude(r => r.User)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<int> GetCountByDrillAsync(Guid drillId)
    {
        return await _dbSet.CountAsync(c => c.DrillId == drillId);
    }
}
