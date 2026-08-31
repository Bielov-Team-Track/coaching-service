using Coaching.Domain.Models.Templates;
using Shared.DataAccess.Repositories.Interfaces;

namespace Coaching.Application.Interfaces.Repositories;

/// <summary>
/// A run's own rows. Adding one to a run the context is already tracking has to go through
/// here rather than through the run's collection alone: BaseEntity assigns an Id in its
/// constructor, so EF reads a child discovered through a navigation as an existing row and
/// saves it as an UPDATE that matches nothing. Add against the set says otherwise.
/// </summary>
public interface ITrainingPlanRunItemRepository : IRepository<TrainingPlanRunItem>
{
}
