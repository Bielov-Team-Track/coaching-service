using Coaching.Application.Interfaces.Repositories;
using Coaching.Domain.Models.Templates;
using Coaching.Infrastructure.Data.Context;
using Shared.DataAccess.Repositories;

namespace Coaching.Infrastructure.Repositories;

public class TrainingPlanRunItemRepository : BaseRepository<TrainingPlanRunItem>, ITrainingPlanRunItemRepository
{
    public TrainingPlanRunItemRepository(CoachingDbContext context) : base(context) { }
}
