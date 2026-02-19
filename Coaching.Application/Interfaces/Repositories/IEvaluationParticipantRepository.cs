using Coaching.Domain.Models.Evaluation;
using Shared.DataAccess.Repositories.Interfaces;

namespace Coaching.Application.Interfaces.Repositories;

public interface IEvaluationParticipantRepository : IRepository<EvaluationParticipant>
{
    Task<EvaluationParticipant?> GetBySessionAndPlayerAsync(Guid sessionId, Guid playerId);
    Task<IEnumerable<EvaluationParticipant>> GetBySessionIdAsync(Guid sessionId);
}
