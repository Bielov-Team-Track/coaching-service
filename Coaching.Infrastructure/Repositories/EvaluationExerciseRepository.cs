using Coaching.Application.Interfaces.Repositories;
using Coaching.Domain.Models.Evaluation;
using Coaching.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
using Shared.DataAccess.Repositories;

namespace Coaching.Infrastructure.Repositories;

public class EvaluationExerciseRepository : BaseRepository<EvaluationExercise>, IEvaluationExerciseRepository
{
    public EvaluationExerciseRepository(CoachingDbContext context) : base(context)
    {
    }

    public async Task<EvaluationExercise?> GetByIdWithMetricsAsync(Guid id)
    {
        return await _dbSet
            .AsSplitQuery()
            .AsNoTracking()
            .Include(e => e.Metrics.Where(m => !m.IsDeleted).OrderBy(m => m.Order))
                .ThenInclude(m => m.SkillWeights.Where(w => !w.IsDeleted))
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);
    }

    public async Task<IEnumerable<EvaluationExercise>> GetByClubIdAsync(Guid clubId)
    {
        return await _dbSet
            .Include(e => e.Metrics.Where(m => !m.IsDeleted).OrderBy(m => m.Order))
            .Where(e => e.ClubId == clubId && !e.IsDeleted)
            .OrderBy(e => e.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<EvaluationExercise>> GetPublicExercisesAsync(int page = 1, int pageSize = 20)
    {
        return await _dbSet
            .Include(e => e.Metrics.Where(m => !m.IsDeleted).OrderBy(m => m.Order))
            .Where(e => e.ClubId == null && !e.IsDeleted)
            .OrderBy(e => e.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }
}
