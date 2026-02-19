using Coaching.Application.Interfaces.Repositories;
using Coaching.Domain.Models.Evaluation;
using Coaching.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
using Shared.DataAccess.Repositories;

namespace Coaching.Infrastructure.Repositories;

public class EvaluationParticipantRepository : BaseRepository<EvaluationParticipant>, IEvaluationParticipantRepository
{
    public EvaluationParticipantRepository(CoachingDbContext context) : base(context)
    {
    }

    public async Task<EvaluationParticipant?> GetBySessionAndPlayerAsync(Guid sessionId, Guid playerId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(p => p.EvaluationSessionId == sessionId
                                   && p.PlayerId == playerId
                                   && !p.IsDeleted);
    }

    public async Task<IEnumerable<EvaluationParticipant>> GetBySessionIdAsync(Guid sessionId)
    {
        return await _dbSet
            .Include(p => p.Evaluation)
            .Where(p => p.EvaluationSessionId == sessionId && !p.IsDeleted)
            .ToListAsync();
    }
}
