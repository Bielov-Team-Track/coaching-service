using Coaching.Application.Interfaces.Repositories;
using Coaching.Domain.Models.Feedback;
using Coaching.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
using Shared.DataAccess.Repositories;

namespace Coaching.Infrastructure.Repositories;

public class FeedbackRepository : BaseRepository<Feedback>, IFeedbackRepository
{
    public FeedbackRepository(CoachingDbContext context) : base(context)
    {
    }

    public async Task<Feedback?> GetByIdWithDetailsAsync(Guid id)
    {
        return await _dbSet
            .AsSplitQuery()
            .Include(f => f.ImprovementPoints.Where(ip => !ip.IsDeleted).OrderBy(ip => ip.Order))
                .ThenInclude(ip => ip.AttachedDrills.Where(d => !d.IsDeleted))
            .Include(f => f.ImprovementPoints)
                .ThenInclude(ip => ip.MediaLinks.Where(m => !m.IsDeleted))
            .Include(f => f.Praise)
            .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);
    }

    public async Task<IEnumerable<Feedback>> GetByRecipientIdAsync(Guid userId, int page = 1, int pageSize = 20)
    {
        return await _dbSet
            .Include(f => f.ImprovementPoints.Where(ip => !ip.IsDeleted).OrderBy(ip => ip.Order))
            .Include(f => f.Praise)
            .Where(f => f.RecipientUserId == userId && f.SharedWithPlayer && !f.IsDeleted)
            .OrderByDescending(f => f.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<Feedback>> GetByCoachIdAsync(Guid userId, int page = 1, int pageSize = 20)
    {
        return await _dbSet
            .Include(f => f.ImprovementPoints.Where(ip => !ip.IsDeleted).OrderBy(ip => ip.Order))
            .Include(f => f.Praise)
            .Where(f => f.CoachUserId == userId && !f.IsDeleted)
            .OrderByDescending(f => f.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<Feedback>> GetByEventIdAsync(Guid eventId)
    {
        return await _dbSet
            .Include(f => f.ImprovementPoints.Where(ip => !ip.IsDeleted).OrderBy(ip => ip.Order))
            .Include(f => f.Praise)
            .Where(f => f.EventId == eventId && !f.IsDeleted)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
    }
}
