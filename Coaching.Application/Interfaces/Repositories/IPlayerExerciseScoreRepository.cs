using Coaching.Domain.Models.Evaluation;
using Shared.DataAccess.Repositories.Interfaces;

namespace Coaching.Application.Interfaces.Repositories;

public interface IPlayerExerciseScoreRepository : IRepository<PlayerExerciseScore>
{
    Task<PlayerExerciseScore?> GetBySessionPlayerExerciseAsync(Guid sessionId, Guid playerId, Guid exerciseId);
    Task<IEnumerable<PlayerExerciseScore>> GetBySessionIdAsync(Guid sessionId);
    Task<IEnumerable<PlayerExerciseScore>> GetBySessionAndExerciseAsync(Guid sessionId, Guid exerciseId);
}
