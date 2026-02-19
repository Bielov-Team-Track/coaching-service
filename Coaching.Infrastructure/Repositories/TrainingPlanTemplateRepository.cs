using Coaching.Application.Interfaces.Repositories;
using Coaching.Domain.Enums;
using Coaching.Domain.Models.Templates;
using Coaching.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
using Shared.DataAccess.Repositories;

namespace Coaching.Infrastructure.Repositories;

public class TrainingPlanRepository : BaseRepository<TrainingPlan>, ITrainingPlanRepository
{
    public TrainingPlanRepository(CoachingDbContext context) : base(context) { }

    public async Task<TrainingPlan?> GetByIdWithDetailsAsync(Guid id)
    {
        return await _dbSet
            .Include(t => t.Sections.OrderBy(s => s.Order))
            .Include(t => t.Items.OrderBy(i => i.Order))
                .ThenInclude(i => i.Drill)
            .Include(t => t.Creator)
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
    }

    public async Task<IEnumerable<TrainingPlan>> GetByUserAsync(Guid userId, int skip, int take)
    {
        return await _dbSet
            .Where(t => t.CreatedByUserId == userId && !t.IsDeleted)
            .Include(t => t.Items)
                .ThenInclude(i => i.Drill)
            .Include(t => t.Creator)
            .OrderByDescending(t => t.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<IEnumerable<TrainingPlan>> GetByClubAsync(Guid clubId, int skip, int take)
    {
        return await _dbSet
            .Where(t => t.ClubId == clubId && !t.IsDeleted)
            .Include(t => t.Items)
                .ThenInclude(i => i.Drill)
            .Include(t => t.Creator)
            .OrderByDescending(t => t.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<IEnumerable<TrainingPlan>> GetPublicAsync(int skip, int take, string? searchTerm = null)
    {
        var query = _dbSet
            .Where(t => t.Visibility == TemplateVisibility.Public && !t.IsDeleted);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(t => t.Name.ToLower().Contains(term) ||
                                    (t.Description != null && t.Description.ToLower().Contains(term)));
        }

        return await query
            .Include(t => t.Items)
                .ThenInclude(i => i.Drill)
            .Include(t => t.Creator)
            .OrderByDescending(t => t.LikeCount)
            .ThenByDescending(t => t.UsageCount)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<int> GetCountByUserAsync(Guid userId)
    {
        return await _dbSet.CountAsync(t => t.CreatedByUserId == userId && !t.IsDeleted);
    }

    public async Task<int> GetCountByClubAsync(Guid clubId)
    {
        return await _dbSet.CountAsync(t => t.ClubId == clubId && !t.IsDeleted);
    }

    public async Task<int> GetPublicCountAsync(string? searchTerm = null)
    {
        var query = _dbSet.Where(t => t.Visibility == TemplateVisibility.Public && !t.IsDeleted);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(t => t.Name.ToLower().Contains(term) ||
                                    (t.Description != null && t.Description.ToLower().Contains(term)));
        }

        return await query.CountAsync();
    }
}
