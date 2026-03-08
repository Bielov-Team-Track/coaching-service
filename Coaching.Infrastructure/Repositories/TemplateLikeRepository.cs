using Coaching.Application.Interfaces.Repositories;
using Coaching.Domain.Models.Templates;
using Coaching.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
using Shared.DataAccess.Repositories;

namespace Coaching.Infrastructure.Repositories;

public class PlanLikeRepository : BaseRepository<PlanLike>, IPlanLikeRepository
{
    public PlanLikeRepository(CoachingDbContext context) : base(context) { }

    public async Task<PlanLike?> GetByTemplateAndUserAsync(Guid templateId, Guid userId)
    {
        return await _dbSet.FirstOrDefaultAsync(l => l.TemplateId == templateId && l.UserId == userId && !l.IsDeleted);
    }

    public async Task<int> GetCountByTemplateAsync(Guid templateId)
    {
        return await _dbSet.CountAsync(l => l.TemplateId == templateId && !l.IsDeleted);
    }
}
