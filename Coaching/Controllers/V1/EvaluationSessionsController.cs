using Asp.Versioning;
using Coaching.Application.DTOs.Evaluation;
using Coaching.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.DataAccess.Providers.Interfaces;

namespace Coaching.Controllers.V1;

[ApiVersion("1.0")]
[Route("v{version:apiVersion}")]
public class EvaluationSessionsController : Shared.Microservices.Controllers.BaseApiController
{
    private readonly IEvaluationSessionService _sessionService;

    public EvaluationSessionsController(IEvaluationSessionService sessionService, IJwtPayloadProvider jwtPayloadProvider)
        : base(jwtPayloadProvider)
    {
        _sessionService = sessionService;
    }

    [HttpGet("evaluation-sessions/me")]
    public async Task<IActionResult> GetMySessions([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        CheckIsUserLoggedIn();
        var sessions = await _sessionService.GetMySessionsAsync(JwtPayload.UserId, page, pageSize);
        return Ok(sessions);
    }

    [HttpGet("clubs/{clubId:guid}/evaluation-sessions")]
    public async Task<IActionResult> GetByClubId(Guid clubId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var sessions = await _sessionService.GetByClubIdAsync(clubId, page, pageSize);
        return Ok(sessions);
    }

    [HttpGet("evaluation-sessions/{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var session = await _sessionService.GetByIdAsync(id);
        if (session == null) return NotFound();
        return Ok(session);
    }

    [HttpPost("evaluation-sessions")]
    public async Task<IActionResult> Create([FromBody] CreateEvaluationSessionDto request)
    {
        CheckIsUserLoggedIn();
        var session = await _sessionService.CreateAsync(request, JwtPayload.UserId);
        return CreatedAtAction(nameof(GetById), new { id = session.Id, version = "1.0" }, session);
    }

    [HttpPut("evaluation-sessions/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEvaluationSessionDto request)
    {
        CheckIsUserLoggedIn();
        var session = await _sessionService.UpdateAsync(id, request, JwtPayload.UserId);
        return Ok(session);
    }

    [HttpDelete("evaluation-sessions/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        CheckIsUserLoggedIn();
        await _sessionService.DeleteAsync(id, JwtPayload.UserId);
        return NoContent();
    }

    [HttpPost("evaluation-sessions/{id:guid}/participants")]
    public async Task<IActionResult> AddParticipants(Guid id, [FromBody] AddParticipantsDto request)
    {
        CheckIsUserLoggedIn();
        var session = await _sessionService.AddParticipantsAsync(id, request, JwtPayload.UserId);
        return Ok(session);
    }

    [HttpDelete("evaluation-sessions/{id:guid}/participants/{participantId:guid}")]
    public async Task<IActionResult> RemoveParticipant(Guid id, Guid participantId)
    {
        CheckIsUserLoggedIn();
        var session = await _sessionService.RemoveParticipantAsync(id, participantId, JwtPayload.UserId);
        return Ok(session);
    }
}
