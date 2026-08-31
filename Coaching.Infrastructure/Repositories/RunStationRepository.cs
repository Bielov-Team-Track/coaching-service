using Coaching.Application.Interfaces.Repositories;
using Coaching.Domain.Models.Templates;
using Coaching.Infrastructure.Data.Context;
using Shared.DataAccess.Repositories;

namespace Coaching.Infrastructure.Repositories;

public class RunStationRepository : BaseRepository<RunStation>, IRunStationRepository
{
    public RunStationRepository(CoachingDbContext context) : base(context) { }
}
