using Coaching.Domain.Models.Templates;
using Shared.DataAccess.Repositories.Interfaces;

namespace Coaching.Application.Interfaces.Repositories;

public interface IPlanLikeRepository : IRepository<PlanLike>
{
    Task<PlanLike?> GetByTemplateAndUserAsync(Guid templateId, Guid userId);
    Task<int> GetCountByTemplateAsync(Guid templateId);
}
