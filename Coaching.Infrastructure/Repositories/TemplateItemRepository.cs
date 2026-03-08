using Coaching.Application.Interfaces.Repositories;
using Coaching.Domain.Models.Templates;
using Coaching.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
using Shared.DataAccess.Repositories;

namespace Coaching.Infrastructure.Repositories;

public class PlanItemRepository : BaseRepository<PlanItem>, IPlanItemRepository
{
    public PlanItemRepository(CoachingDbContext context) : base(context) { }

    public async Task<IEnumerable<PlanItem>> GetByTemplateAsync(Guid templateId)
    {
        return await _dbSet
            .Where(i => i.TemplateId == templateId && !i.IsDeleted)
            .OrderBy(i => i.Order)
            .ToListAsync();
    }

    public async Task<int> GetMaxOrderAsync(Guid templateId)
    {
        var maxOrder = await _dbSet
            .Where(i => i.TemplateId == templateId && !i.IsDeleted)
            .MaxAsync(i => (int?)i.Order);
        return maxOrder ?? 0;
    }
}
