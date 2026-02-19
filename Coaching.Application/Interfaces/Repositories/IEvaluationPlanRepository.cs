using Coaching.Domain.Models.Evaluation;
using Shared.DataAccess.Repositories.Interfaces;

namespace Coaching.Application.Interfaces.Repositories;

public interface IEvaluationPlanRepository : IRepository<EvaluationPlan>
{
    Task<EvaluationPlan?> GetByIdWithItemsAsync(Guid id);
    Task<List<EvaluationPlan>> GetByClubIdAsync(Guid clubId);
    Task<List<EvaluationPlan>> GetByUserIdAsync(Guid userId);
}
