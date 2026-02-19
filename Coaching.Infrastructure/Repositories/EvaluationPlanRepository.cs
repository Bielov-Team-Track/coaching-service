using Coaching.Application.Interfaces.Repositories;
using Coaching.Domain.Models.Evaluation;
using Coaching.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
using Shared.DataAccess.Repositories;

namespace Coaching.Infrastructure.Repositories;

public class EvaluationPlanRepository : BaseRepository<EvaluationPlan>, IEvaluationPlanRepository
{
    public EvaluationPlanRepository(CoachingDbContext context) : base(context)
    {
    }

    public async Task<EvaluationPlan?> GetByIdWithItemsAsync(Guid id)
    {
        return await _dbSet
            .AsSplitQuery()
            .AsNoTracking()
            .Include(p => p.Items.Where(i => !i.IsDeleted).OrderBy(i => i.Order))
                .ThenInclude(i => i.Exercise)
                    .ThenInclude(e => e.Metrics.Where(m => !m.IsDeleted).OrderBy(m => m.Order))
                        .ThenInclude(m => m.SkillWeights.Where(w => !w.IsDeleted))
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
    }

    public async Task<List<EvaluationPlan>> GetByClubIdAsync(Guid clubId)
    {
        return await _dbSet
            .AsSplitQuery()
            .AsNoTracking()
            .Include(p => p.Items.Where(i => !i.IsDeleted).OrderBy(i => i.Order))
                .ThenInclude(i => i.Exercise)
                    .ThenInclude(e => e.Metrics.Where(m => !m.IsDeleted).OrderBy(m => m.Order))
                        .ThenInclude(m => m.SkillWeights.Where(w => !w.IsDeleted))
            .Where(p => p.ClubId == clubId && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<EvaluationPlan>> GetByUserIdAsync(Guid userId)
    {
        return await _dbSet
            .AsSplitQuery()
            .AsNoTracking()
            .Include(p => p.Items.Where(i => !i.IsDeleted).OrderBy(i => i.Order))
                .ThenInclude(i => i.Exercise)
                    .ThenInclude(e => e.Metrics.Where(m => !m.IsDeleted).OrderBy(m => m.Order))
                        .ThenInclude(m => m.SkillWeights.Where(w => !w.IsDeleted))
            .Where(p => p.CreatedByUserId == userId && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }
}
