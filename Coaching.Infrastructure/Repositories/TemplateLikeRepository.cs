using Coaching.Application.Interfaces.Repositories;
using Coaching.Domain.Models.Templates;
using Coaching.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
using Shared.DataAccess.Repositories;

namespace Coaching.Infrastructure.Repositories;

public class PlanLikeRepository : BaseRepository<PlanLike>, IPlanLikeRepository
{
    public PlanLikeRepository(CoachingDbContext context) : base(context) { }

    public async Task<PlanLike?> GetByTemplateAndUserAsync(Guid templateId, Guid userId)
    {
        return await _dbSet.FirstOrDefaultAsync(l => l.TemplateId == templateId && l.UserId == userId && !l.IsDeleted);
    }

    public async Task<int> GetCountByTemplateAsync(Guid templateId)
    {
        return await _dbSet.CountAsync(l => l.TemplateId == templateId && !l.IsDeleted);
    }

    public async Task<IEnumerable<Guid>> GetUserLikedPlanIdsAsync(Guid userId, IEnumerable<Guid> planIds)
    {
        var ids = planIds.ToList();
        return await _dbSet
            .Where(l => l.UserId == userId && !l.IsDeleted && ids.Contains(l.TemplateId))
            .Select(l => l.TemplateId)
            .ToListAsync();
    }

    public async Task<IEnumerable<PlanLike>> GetByUserAsync(Guid userId, int skip, int take)
    {
        return await _dbSet
            .Where(l => l.UserId == userId && !l.IsDeleted)
            .Include(l => l.Plan)
                .ThenInclude(p => p.Items)
            .Include(l => l.Plan)
                .ThenInclude(p => p.Creator)
            .OrderByDescending(l => l.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<int> GetCountByUserAsync(Guid userId)
    {
        return await _dbSet.CountAsync(l => l.UserId == userId && !l.IsDeleted);
    }
}
