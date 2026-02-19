using Coaching.Application.Interfaces.Repositories;
using Coaching.Domain.Models.Drills;
using Grpc.Core;
using Shared.Contracts.Grpc;
using Shared.DataAccess.Repositories.Interfaces;

namespace Coaching.Grpc;

public class CoachingInternalServiceImpl : CoachingInternalService.CoachingInternalServiceBase
{
    private readonly IDrillRepository _drillRepository;

    public CoachingInternalServiceImpl(IDrillRepository drillRepository)
    {
        _drillRepository = drillRepository;
    }

    public override async Task<ValidateDrillExistsResponse> ValidateDrillExists(
        ValidateDrillExistsRequest request,
        ServerCallContext context)
    {
        var drillId = Guid.Parse(request.DrillId);
        var drill = await _drillRepository.GetByIdAsync(drillId);

        if (drill == null)
        {
            return new ValidateDrillExistsResponse { Exists = false };
        }

        return new ValidateDrillExistsResponse
        {
            Exists = true,
            DrillName = drill.Name,
            Category = drill.Category.ToString(),
            EstimatedDuration = drill.Duration ?? 0,
            Level = drill.Intensity.ToString()
        };
    }

    public override async Task<GetDrillsListResponse> GetDrillsList(
        GetDrillsListRequest request,
        ServerCallContext context)
    {
        var drillIds = request.DrillIds.Select(Guid.Parse).ToList();
        var response = new GetDrillsListResponse();

        foreach (var drillId in drillIds)
        {
            var drill = await _drillRepository.GetByIdAsync(drillId);
            if (drill != null)
            {
                var thumbnailUrl = drill.Attachments?
                    .OrderBy(a => a.Order)
                    .FirstOrDefault()?.FileUrl ?? "";

                response.Drills.Add(new DrillInfo
                {
                    Id = drill.Id.ToString(),
                    Name = drill.Name,
                    Category = drill.Category.ToString(),
                    EstimatedDuration = drill.Duration ?? 0,
                    Level = drill.Intensity.ToString(),
                    ThumbnailUrl = thumbnailUrl
                });
            }
        }

        return response;
    }
}
