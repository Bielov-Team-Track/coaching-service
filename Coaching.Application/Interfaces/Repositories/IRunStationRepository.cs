using Coaching.Domain.Models.Templates;
using Shared.DataAccess.Repositories.Interfaces;

namespace Coaching.Application.Interfaces.Repositories;

/// <summary>
/// The run's own copy of a Stations row's groups. A restart replaces them, and replacing a
/// tracked parent's children needs the intent stated against the set — see
/// <see cref="ITrainingPlanRunItemRepository"/> for why.
/// </summary>
public interface IRunStationRepository : IRepository<RunStation>
{
}
