using Asp.Versioning;
using Coaching.Application.DTOs.Feedback;
using Coaching.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.DataAccess.Providers.Interfaces;

namespace Coaching.Controllers;

[ApiVersion("1.0")]
[Route("v{version:apiVersion}")]
public class FeedbackController : Shared.Microservices.Controllers.BaseApiController
{
    private readonly IFeedbackService _feedbackService;

    public FeedbackController(IFeedbackService feedbackService, IJwtPayloadProvider jwtPayloadProvider)
        : base(jwtPayloadProvider)
    {
        _feedbackService = feedbackService;
    }

    [HttpGet("feedback/{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        CheckIsUserLoggedIn();
        var feedback = await _feedbackService.GetByIdAsync(id, JwtPayload.UserId);
        if (feedback == null) return NotFound();
        return Ok(feedback);
    }

    [HttpGet("events/{eventId:guid}/feedback")]
    public async Task<IActionResult> GetByEventId(Guid eventId)
    {
        CheckIsUserLoggedIn();
        var feedbacks = await _feedbackService.GetByEventIdAsync(eventId, JwtPayload.UserId);
        return Ok(feedbacks);
    }

    [HttpGet("me/feedback/received")]
    public async Task<IActionResult> GetReceivedFeedback([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        CheckIsUserLoggedIn();
        var result = await _feedbackService.GetReceivedFeedbackAsync(JwtPayload.UserId, page, pageSize);
        return Ok(result);
    }

    [HttpGet("me/feedback/given")]
    public async Task<IActionResult> GetGivenFeedback([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        CheckIsUserLoggedIn();
        var result = await _feedbackService.GetGivenFeedbackAsync(JwtPayload.UserId, page, pageSize);
        return Ok(result);
    }

    [HttpPost("feedback")]
    public async Task<IActionResult> Create([FromBody] CreateFeedbackDto request)
    {
        CheckIsUserLoggedIn();
        var feedback = await _feedbackService.CreateAsync(request, JwtPayload.UserId);
        return CreatedAtAction(nameof(GetById), new { id = feedback.Id, version = "1.0" }, feedback);
    }

    [HttpPut("feedback/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFeedbackDto request)
    {
        CheckIsUserLoggedIn();
        var feedback = await _feedbackService.UpdateAsync(id, request, JwtPayload.UserId);
        return Ok(feedback);
    }

    [HttpDelete("feedback/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        CheckIsUserLoggedIn();
        await _feedbackService.DeleteAsync(id, JwtPayload.UserId);
        return NoContent();
    }

    [HttpPut("feedback/{id:guid}/share")]
    public async Task<IActionResult> ShareWithPlayer(Guid id, [FromQuery] bool share = true)
    {
        CheckIsUserLoggedIn();
        var feedback = await _feedbackService.ShareWithPlayerAsync(id, share, JwtPayload.UserId);
        return Ok(feedback);
    }

    [HttpPost("feedback/{id:guid}/improvement-points")]
    public async Task<IActionResult> AddImprovementPoint(Guid id, [FromBody] AddImprovementPointDto request)
    {
        CheckIsUserLoggedIn();
        var feedback = await _feedbackService.AddImprovementPointAsync(id, request, JwtPayload.UserId);
        return Ok(feedback);
    }

    [HttpPut("feedback/{id:guid}/improvement-points/{pointId:guid}")]
    public async Task<IActionResult> UpdateImprovementPoint(Guid id, Guid pointId, [FromBody] UpdateImprovementPointDto request)
    {
        CheckIsUserLoggedIn();
        var feedback = await _feedbackService.UpdateImprovementPointAsync(id, pointId, request, JwtPayload.UserId);
        return Ok(feedback);
    }

    [HttpDelete("feedback/{id:guid}/improvement-points/{pointId:guid}")]
    public async Task<IActionResult> RemoveImprovementPoint(Guid id, Guid pointId)
    {
        CheckIsUserLoggedIn();
        var feedback = await _feedbackService.RemoveImprovementPointAsync(id, pointId, JwtPayload.UserId);
        return Ok(feedback);
    }

    [HttpPost("feedback/{id:guid}/improvement-points/{pointId:guid}/drills/{drillId:guid}")]
    public async Task<IActionResult> AddDrillToPoint(Guid id, Guid pointId, Guid drillId)
    {
        CheckIsUserLoggedIn();
        var feedback = await _feedbackService.AddDrillToPointAsync(id, pointId, drillId, JwtPayload.UserId);
        return Ok(feedback);
    }

    [HttpDelete("feedback/{id:guid}/improvement-points/{pointId:guid}/drills/{drillId:guid}")]
    public async Task<IActionResult> RemoveDrillFromPoint(Guid id, Guid pointId, Guid drillId)
    {
        CheckIsUserLoggedIn();
        var feedback = await _feedbackService.RemoveDrillFromPointAsync(id, pointId, drillId, JwtPayload.UserId);
        return Ok(feedback);
    }

    [HttpPost("feedback/{id:guid}/improvement-points/{pointId:guid}/media")]
    public async Task<IActionResult> AddMediaToPoint(Guid id, Guid pointId, [FromBody] CreateImprovementPointMediaDto request)
    {
        CheckIsUserLoggedIn();
        var feedback = await _feedbackService.AddMediaToPointAsync(id, pointId, request, JwtPayload.UserId);
        return Ok(feedback);
    }

    [HttpDelete("feedback/{id:guid}/improvement-points/{pointId:guid}/media/{mediaId:guid}")]
    public async Task<IActionResult> RemoveMediaFromPoint(Guid id, Guid pointId, Guid mediaId)
    {
        CheckIsUserLoggedIn();
        var feedback = await _feedbackService.RemoveMediaFromPointAsync(id, pointId, mediaId, JwtPayload.UserId);
        return Ok(feedback);
    }

    [HttpPost("feedback/{id:guid}/praise")]
    public async Task<IActionResult> AddPraise(Guid id, [FromBody] CreatePraiseDto request)
    {
        CheckIsUserLoggedIn();
        var feedback = await _feedbackService.AddPraiseAsync(id, request, JwtPayload.UserId);
        return Ok(feedback);
    }

    [HttpPut("feedback/{id:guid}/praise")]
    public async Task<IActionResult> UpdatePraise(Guid id, [FromBody] UpdatePraiseDto request)
    {
        CheckIsUserLoggedIn();
        var feedback = await _feedbackService.UpdatePraiseAsync(id, request, JwtPayload.UserId);
        return Ok(feedback);
    }

    [HttpDelete("feedback/{id:guid}/praise")]
    public async Task<IActionResult> RemovePraise(Guid id)
    {
        CheckIsUserLoggedIn();
        var feedback = await _feedbackService.RemovePraiseAsync(id, JwtPayload.UserId);
        return Ok(feedback);
    }
}
