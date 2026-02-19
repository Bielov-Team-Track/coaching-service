using Coaching.Domain.Enums;
using Coaching.Domain.Models.Templates;
using Shared.DataAccess.Repositories.Interfaces;

namespace Coaching.Application.Interfaces.Repositories;

public interface ITrainingPlanRepository : IRepository<TrainingPlan>
{
    Task<TrainingPlan?> GetByIdWithDetailsAsync(Guid id);
    Task<IEnumerable<TrainingPlan>> GetByUserAsync(Guid userId, int skip, int take);
    Task<IEnumerable<TrainingPlan>> GetByClubAsync(Guid clubId, int skip, int take);
    Task<IEnumerable<TrainingPlan>> GetPublicAsync(int skip, int take, string? searchTerm = null);
    Task<int> GetCountByUserAsync(Guid userId);
    Task<int> GetCountByClubAsync(Guid clubId);
    Task<int> GetPublicCountAsync(string? searchTerm = null);
}
