using Coaching.Domain.Models.Drills;
using Shared.DataAccess.Repositories.Interfaces;

namespace Coaching.Application.Interfaces.Repositories;

public interface IDrillBookmarkRepository : IRepository<DrillBookmark>
{
    Task<DrillBookmark?> GetByDrillAndUserAsync(Guid drillId, Guid userId);
    Task<IEnumerable<DrillBookmark>> GetByUserAsync(Guid userId);
    Task<IEnumerable<Guid>> GetUserBookmarkedDrillIdsAsync(Guid userId, IEnumerable<Guid> drillIds);
    Task<Dictionary<Guid, int>> GetBookmarkCountsAsync(IEnumerable<Guid> drillIds);
}
