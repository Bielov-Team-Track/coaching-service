using Coaching.Application.Interfaces.Repositories;
using Coaching.Domain.Models.Templates;
using Coaching.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
using Shared.DataAccess.Repositories;

namespace Coaching.Infrastructure.Repositories;

public class PlanCommentRepository : BaseRepository<PlanComment>, IPlanCommentRepository
{
    public PlanCommentRepository(CoachingDbContext context) : base(context) { }

    public async Task<PlanComment?> GetByIdWithDetailsAsync(Guid id)
    {
        return await _dbSet
            .Include(c => c.User)
            .Include(c => c.Replies.Where(r => !r.IsDeleted))
                .ThenInclude(r => r.User)
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
    }

    public async Task<IEnumerable<PlanComment>> GetByTemplateWithCursorAsync(Guid templateId, Guid? cursor, int limit)
    {
        var query = _dbSet
            .Where(c => c.TemplateId == templateId && c.ParentCommentId == null && !c.IsDeleted)
            .Include(c => c.User)
            .Include(c => c.Replies.Where(r => !r.IsDeleted))
                .ThenInclude(r => r.User)
            .OrderByDescending(c => c.CreatedAt)
            .ThenByDescending(c => c.Id);

        if (cursor.HasValue)
        {
            var cursorComment = await _dbSet.FindAsync(cursor.Value);
            if (cursorComment != null)
            {
                query = (IOrderedQueryable<PlanComment>)query
                    .Where(c => c.CreatedAt < cursorComment.CreatedAt ||
                               (c.CreatedAt == cursorComment.CreatedAt && c.Id < cursorComment.Id));
            }
        }

        return await query.Take(limit + 1).ToListAsync();
    }

    public async Task<int> GetCountByTemplateAsync(Guid templateId)
    {
        return await _dbSet.CountAsync(c => c.TemplateId == templateId && !c.IsDeleted);
    }
}
