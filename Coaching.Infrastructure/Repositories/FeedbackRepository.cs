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
                    .ThenInclude(ad => ad.Drill)
            .Include(f => f.ImprovementPoints)
                .ThenInclude(ip => ip.MediaLinks.Where(m => !m.IsDeleted))
            .Include(f => f.Media.Where(m => !m.IsDeleted).OrderBy(m => m.Order))
            .Include(f => f.Praise)
            .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);
    }

    /// <summary>
    /// Split, like the detail read: improvement points and attachments are two collections, and
    /// one statement carrying both returns their product — every point against every attachment.
    /// Id breaks ties in the ordering because a split query pages each collection separately, and
    /// CreatedAt alone does not decide which rows a page holds when two share a timestamp.
    /// </summary>
    public async Task<IEnumerable<Feedback>> GetByRecipientIdAsync(Guid userId, int page = 1, int pageSize = 20)
    {
        return await _dbSet
            .AsSplitQuery()
            .Include(f => f.ImprovementPoints.Where(ip => !ip.IsDeleted).OrderBy(ip => ip.Order))
            .Include(f => f.Media.Where(m => !m.IsDeleted).OrderBy(m => m.Order))
            .Include(f => f.Praise)
            .Where(f => f.RecipientUserId == userId && f.SharedWithPlayer && !f.IsDeleted)
            .OrderByDescending(f => f.CreatedAt)
            .ThenBy(f => f.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<Feedback>> GetByCoachIdAsync(Guid userId, int page = 1, int pageSize = 20)
    {
        return await _dbSet
            .AsSplitQuery()
            .Include(f => f.ImprovementPoints.Where(ip => !ip.IsDeleted).OrderBy(ip => ip.Order))
            .Include(f => f.Media.Where(m => !m.IsDeleted).OrderBy(m => m.Order))
            .Include(f => f.Praise)
            .Where(f => f.CoachUserId == userId && !f.IsDeleted)
            .OrderByDescending(f => f.CreatedAt)
            .ThenBy(f => f.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<Feedback>> GetByEventIdAsync(Guid eventId)
    {
        return await _dbSet
            .AsSplitQuery()
            .Include(f => f.ImprovementPoints.Where(ip => !ip.IsDeleted).OrderBy(ip => ip.Order))
            .Include(f => f.Media.Where(m => !m.IsDeleted).OrderBy(m => m.Order))
            .Include(f => f.Praise)
            .Where(f => f.EventId == eventId && !f.IsDeleted)
            .OrderByDescending(f => f.CreatedAt)
            .ThenBy(f => f.Id)
            .ToListAsync();
    }

    public async Task<int> GetUnseenCountAsync(Guid recipientUserId)
    {
        return await _dbSet
            .CountAsync(f => f.RecipientUserId == recipientUserId
                && f.SharedWithPlayer
                && f.SeenAt == null
                && !f.IsDeleted);
    }
}
