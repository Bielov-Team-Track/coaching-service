using Coaching.Application.Interfaces.Repositories;
using Coaching.Domain.Models.Evaluation;
using Coaching.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
using Shared.DataAccess.Repositories;

namespace Coaching.Infrastructure.Repositories;

public class PlayerExerciseScoreRepository : BaseRepository<PlayerExerciseScore>, IPlayerExerciseScoreRepository
{
    public PlayerExerciseScoreRepository(CoachingDbContext context) : base(context)
    {
    }

    public async Task<PlayerExerciseScore?> GetBySessionPlayerExerciseAsync(Guid sessionId, Guid playerId, Guid exerciseId)
    {
        return await _dbSet
            .Where(s => s.SessionId == sessionId && s.PlayerId == playerId && s.ExerciseId == exerciseId && !s.IsDeleted)
            .Include(s => s.MetricScores.Where(m => !m.IsDeleted))
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<PlayerExerciseScore>> GetBySessionIdAsync(Guid sessionId)
    {
        return await _dbSet
            .Where(s => s.SessionId == sessionId && !s.IsDeleted)
            .Include(s => s.MetricScores.Where(m => !m.IsDeleted))
            .ToListAsync();
    }

    public async Task<IEnumerable<PlayerExerciseScore>> GetBySessionAndExerciseAsync(Guid sessionId, Guid exerciseId)
    {
        return await _dbSet
            .Where(s => s.SessionId == sessionId && s.ExerciseId == exerciseId && !s.IsDeleted)
            .Include(s => s.MetricScores.Where(m => !m.IsDeleted))
            .ToListAsync();
    }
}
