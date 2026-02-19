using Coaching.Application.Interfaces.Repositories;
using Coaching.Domain.Models.Templates;
using Coaching.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
using Shared.DataAccess.Repositories;

namespace Coaching.Infrastructure.Repositories;

public class PlanSectionRepository : BaseRepository<PlanSection>, IPlanSectionRepository
{
    public PlanSectionRepository(CoachingDbContext context) : base(context) { }

    public async Task<IEnumerable<PlanSection>> GetByTemplateAsync(Guid templateId)
    {
        return await _dbSet
            .Where(s => s.TemplateId == templateId && !s.IsDeleted)
            .OrderBy(s => s.Order)
            .ToListAsync();
    }
}
