using Coaching.Domain.Models.Evaluation;
using Shared.DataAccess.Repositories.Interfaces;

namespace Coaching.Application.Interfaces.Repositories;

public interface IEvaluationSessionRepository : IRepository<EvaluationSession>
{
    Task<EvaluationSession?> GetByIdWithParticipantsAsync(Guid id);
    Task<IEnumerable<EvaluationSession>> GetByClubIdAsync(Guid clubId, int page = 1, int pageSize = 20);
    Task<IEnumerable<EvaluationSession>> GetByCoachUserIdAsync(Guid coachUserId, int page = 1, int pageSize = 20);
}
