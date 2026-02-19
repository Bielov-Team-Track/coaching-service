using Coaching.Application.Interfaces.Repositories;
using Coaching.Domain.Models.Drills;
using Coaching.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
using Shared.DataAccess.Repositories;

namespace Coaching.Infrastructure.Repositories;

public class DrillAttachmentRepository : BaseRepository<DrillAttachment>, IDrillAttachmentRepository
{
    public DrillAttachmentRepository(CoachingDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<DrillAttachment>> GetByDrillAsync(Guid drillId)
    {
        return await _dbSet
            .Where(a => a.DrillId == drillId)
            .OrderBy(a => a.Order)
            .ToListAsync();
    }

    public async Task<int> GetMaxOrderForDrillAsync(Guid drillId)
    {
        var maxOrder = await _dbSet
            .Where(a => a.DrillId == drillId)
            .MaxAsync(a => (int?)a.Order);
        return maxOrder ?? 0;
    }
}
