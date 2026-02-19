using Coaching.Domain.Models.Templates;
using Shared.DataAccess.Repositories.Interfaces;

namespace Coaching.Application.Interfaces.Repositories;

public interface IPlanItemRepository : IRepository<PlanItem>
{
    Task<IEnumerable<PlanItem>> GetByTemplateAsync(Guid templateId);
    Task<int> GetMaxOrderAsync(Guid templateId);
}
