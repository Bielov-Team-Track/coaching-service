using Asp.Versioning;
using Coaching.Application.DTOs.Evaluation;
using Coaching.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.DataAccess.Providers.Interfaces;

namespace Coaching.Controllers.V1;

[ApiVersion("1.0")]
[Route("v{version:apiVersion}")]
public class PlayerEvaluationsController : Shared.Microservices.Controllers.BaseApiController
{
    private readonly IPlayerEvaluationService _evaluationService;

    public PlayerEvaluationsController(IPlayerEvaluationService evaluationService, IJwtPayloadProvider jwtPayloadProvider)
        : base(jwtPayloadProvider)
    {
        _evaluationService = evaluationService;
    }

    [HttpGet("evaluation-sessions/{sessionId:guid}/evaluations")]
    public async Task<IActionResult> GetSessionSummary(Guid sessionId)
    {
        CheckIsUserLoggedIn();
        var summary = await _evaluationService.GetSessionSummaryAsync(sessionId, JwtPayload.UserId);
        return Ok(summary);
    }

    [HttpGet("evaluations/{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        CheckIsUserLoggedIn();
        var evaluation = await _evaluationService.GetByIdAsync(id, JwtPayload.UserId);
        if (evaluation == null) return NotFound();
        return Ok(evaluation);
    }

    [HttpGet("me/evaluations")]
    public async Task<IActionResult> GetMyEvaluations([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        CheckIsUserLoggedIn();
        var evaluations = await _evaluationService.GetPlayerHistoryAsync(JwtPayload.UserId, page, pageSize);
        return Ok(evaluations);
    }

    [HttpPost("evaluation-sessions/{sessionId:guid}/evaluations")]
    public async Task<IActionResult> Create(Guid sessionId, [FromBody] CreatePlayerEvaluationDto request)
    {
        CheckIsUserLoggedIn();
        var evaluation = await _evaluationService.CreateAsync(sessionId, request, JwtPayload.UserId);
        return CreatedAtAction(nameof(GetById), new { id = evaluation.Id, version = "1.0" }, evaluation);
    }

    [HttpPost("evaluations/{id:guid}/scores")]
    public async Task<IActionResult> RecordScores(Guid id, [FromBody] RecordMetricScoresDto request)
    {
        CheckIsUserLoggedIn();
        var evaluation = await _evaluationService.RecordMetricScoresAsync(id, request, JwtPayload.UserId);
        return Ok(evaluation);
    }

    [HttpPut("evaluations/{id:guid}/outcome")]
    public async Task<IActionResult> UpdateOutcome(Guid id, [FromBody] UpdateEvaluationOutcomeDto request)
    {
        CheckIsUserLoggedIn();
        var evaluation = await _evaluationService.UpdateOutcomeAsync(id, request, JwtPayload.UserId);
        return Ok(evaluation);
    }

    [HttpPut("evaluations/{id:guid}/share")]
    public async Task<IActionResult> ShareWithPlayer(Guid id, [FromQuery] bool share = true)
    {
        CheckIsUserLoggedIn();
        var evaluation = await _evaluationService.ShareWithPlayerAsync(id, share, JwtPayload.UserId);
        return Ok(evaluation);
    }

    [HttpDelete("evaluations/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        CheckIsUserLoggedIn();
        await _evaluationService.DeleteAsync(id, JwtPayload.UserId);
        return NoContent();
    }
}
