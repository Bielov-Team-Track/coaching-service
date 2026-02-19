using Coaching.Domain.Models.Templates;
using Shared.DataAccess.Repositories.Interfaces;

namespace Coaching.Application.Interfaces.Repositories;

public interface IPlanCommentRepository : IRepository<PlanComment>
{
    Task<PlanComment?> GetByIdWithDetailsAsync(Guid id);
    Task<IEnumerable<PlanComment>> GetByTemplateWithCursorAsync(Guid templateId, Guid? cursor, int limit);
    Task<int> GetCountByTemplateAsync(Guid templateId);
}
