using Coaching.Domain.Models.Templates;
using Shared.DataAccess.Repositories.Interfaces;

namespace Coaching.Application.Interfaces.Repositories;

public interface ITrainingPlanRunRepository : IRepository<TrainingPlanRun>
{
    /// <summary>
    /// Loads the run (with its items ordered by Order) for the instance plan attached to the event.
    /// Returns null when no run has started.
    /// </summary>
    Task<TrainingPlanRun?> GetByEventIdWithDetailsAsync(Guid eventId);
}
