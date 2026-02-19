using Coaching.Domain.Models.Evaluation;
using Shared.DataAccess.Repositories.Interfaces;

namespace Coaching.Application.Interfaces.Repositories;

public interface IPlayerEvaluationRepository : IRepository<PlayerEvaluation>
{
    Task<PlayerEvaluation?> GetByIdWithScoresAsync(Guid id);
    Task<PlayerEvaluation?> GetByParticipantIdAsync(Guid participantId);
    Task<IEnumerable<PlayerEvaluation>> GetBySessionIdAsync(Guid sessionId);
    Task<IEnumerable<PlayerEvaluation>> GetByPlayerIdAsync(Guid playerId, int page = 1, int pageSize = 20);
}
