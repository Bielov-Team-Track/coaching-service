using Coaching.Domain.Models.Templates;
using Shared.DataAccess.Repositories.Interfaces;

namespace Coaching.Application.Interfaces.Repositories;

public interface IPlanSectionRepository : IRepository<PlanSection>
{
    Task<IEnumerable<PlanSection>> GetByTemplateAsync(Guid templateId);
}
