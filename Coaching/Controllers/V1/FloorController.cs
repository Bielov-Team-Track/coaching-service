using Asp.Versioning;
using Coaching.Application.DTOs.Templates;
using Coaching.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.DataAccess.Providers.Interfaces;

namespace Coaching.Controllers.V1;

[ApiVersion("1.0")]
[Route("v{version:apiVersion}")]
public class FloorController : Shared.Microservices.Controllers.BaseApiController
{
    private readonly IPlanFloorService _floorService;

    public FloorController(
        IPlanFloorService floorService,
        IJwtPayloadProvider jwtPayloadProvider)
        : base(jwtPayloadProvider)
    {
        _floorService = floorService;
    }

    [HttpGet("plans/{planId:guid}/floor")]
    public async Task<IActionResult> GetFloor([FromRoute] Guid planId, [FromQuery] Guid venueId)
    {
        CheckIsUserLoggedIn();
        var floor = await _floorService.GetFloorAsync(planId, venueId, JwtPayload.UserId);
        return Ok(floor);
    }

    [HttpPut("plans/{planId:guid}/floor")]
    public async Task<IActionResult> PutFloor(
        [FromRoute] Guid planId,
        [FromQuery] Guid venueId,
        [FromBody] SavePlanFloorDto request)
    {
        CheckIsUserLoggedIn();
        var floor = await _floorService.PutFloorAsync(planId, venueId, request, JwtPayload.UserId);
        return Ok(floor);
    }
}
