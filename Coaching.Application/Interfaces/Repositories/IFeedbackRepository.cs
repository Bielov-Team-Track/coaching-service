using Coaching.Domain.Models.Feedback;
using Shared.DataAccess.Repositories.Interfaces;

namespace Coaching.Application.Interfaces.Repositories;

public interface IFeedbackRepository : IRepository<Feedback>
{
    Task<Feedback?> GetByIdWithDetailsAsync(Guid id);
    Task<IEnumerable<Feedback>> GetByRecipientIdAsync(Guid userId, int page = 1, int pageSize = 20);
    Task<IEnumerable<Feedback>> GetByCoachIdAsync(Guid userId, int page = 1, int pageSize = 20);
    Task<IEnumerable<Feedback>> GetByEventIdAsync(Guid eventId);
}
