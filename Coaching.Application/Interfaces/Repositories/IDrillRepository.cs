using Coaching.Domain.Models.Drills;
using Shared.DataAccess.Repositories.Interfaces;

namespace Coaching.Application.Interfaces.Repositories;

public interface IDrillRepository : IRepository<Drill>
{
    Task<IEnumerable<Drill>> GetByCreatorAsync(Guid userId);
    Task<IEnumerable<Drill>> GetByClubAsync(Guid clubId);
    Task<Drill?> GetByIdWithDetailsAsync(Guid id);
}
