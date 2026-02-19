using Coaching.Domain.Models.Drills;
using Shared.DataAccess.Repositories.Interfaces;

namespace Coaching.Application.Interfaces.Repositories;

public interface IDrillAttachmentRepository : IRepository<DrillAttachment>
{
    Task<IEnumerable<DrillAttachment>> GetByDrillAsync(Guid drillId);
    Task<int> GetMaxOrderForDrillAsync(Guid drillId);
}
