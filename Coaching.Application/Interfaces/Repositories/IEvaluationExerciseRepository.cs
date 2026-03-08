using Coaching.Domain.Models.Evaluation;
using Shared.DataAccess.Repositories.Interfaces;

namespace Coaching.Application.Interfaces.Repositories;

public interface IEvaluationExerciseRepository : IRepository<EvaluationExercise>
{
    Task<EvaluationExercise?> GetByIdWithMetricsAsync(Guid id);
    Task<IEnumerable<EvaluationExercise>> GetByClubIdAsync(Guid clubId);
    Task<IEnumerable<EvaluationExercise>> GetPublicExercisesAsync(int page = 1, int pageSize = 20);
}
