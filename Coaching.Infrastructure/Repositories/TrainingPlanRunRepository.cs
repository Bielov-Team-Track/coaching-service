using Coaching.Application.Interfaces.Repositories;
using Coaching.Domain.Models.Templates;
using Coaching.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
using Shared.DataAccess.Repositories;

namespace Coaching.Infrastructure.Repositories;

public class TrainingPlanRunRepository : BaseRepository<TrainingPlanRun>, ITrainingPlanRunRepository
{
    public TrainingPlanRunRepository(CoachingDbContext context) : base(context) { }

    public async Task<TrainingPlanRun?> GetByEventIdWithDetailsAsync(Guid eventId)
    {
        return await _dbSet
            .Include(r => r.Items.OrderBy(i => i.Order))
            .FirstOrDefaultAsync(r => r.EventId == eventId && !r.IsDeleted);
    }
}
