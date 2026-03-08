using Coaching.Domain.Models.Templates;
using Shared.DataAccess.Repositories.Interfaces;

namespace Coaching.Application.Interfaces.Repositories;

public interface IPlanBookmarkRepository : IRepository<PlanBookmark>
{
    Task<PlanBookmark?> GetByTemplateAndUserAsync(Guid templateId, Guid userId);
    Task<IEnumerable<PlanBookmark>> GetByUserAsync(Guid userId, int skip, int take);
    Task<int> GetCountByUserAsync(Guid userId);
}
