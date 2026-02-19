using Coaching.Domain.Models.Drills;
using Shared.DataAccess.Repositories.Interfaces;

namespace Coaching.Application.Interfaces.Repositories;

public interface IDrillLikeRepository : IRepository<DrillLike>
{
    Task<DrillLike?> GetByDrillAndUserAsync(Guid drillId, Guid userId);
    Task<int> GetCountByDrillAsync(Guid drillId);
    Task<IEnumerable<Guid>> GetUserLikedDrillIdsAsync(Guid userId, IEnumerable<Guid> drillIds);
}
