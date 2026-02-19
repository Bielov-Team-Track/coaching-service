using Asp.Versioning;
using Coaching.Application.DTOs.Export;
using Coaching.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.DataAccess.Providers.Interfaces;

namespace Coaching.Controllers.V1;

[ApiVersion("1.0")]
[Route("v{version:apiVersion}")]
public class ExportController : Shared.Microservices.Controllers.BaseApiController
{
    private readonly IExportService _exportService;

    public ExportController(IExportService exportService, IJwtPayloadProvider jwtPayloadProvider)
        : base(jwtPayloadProvider)
    {
        _exportService = exportService;
    }

    [HttpPost("evaluation-sessions/{sessionId:guid}/evaluations/export")]
    public async Task<IActionResult> ExportSessionEvaluations(
        [FromRoute] Guid sessionId,
        [FromBody] ExportEvaluationRequest request)
    {
        CheckIsUserLoggedIn();
        request.SessionId = sessionId;
        var result = await _exportService.ExportEvaluationsAsync(request, JwtPayload.UserId);
        return File(result.Data, result.ContentType, result.FileName);
    }

    [HttpPost("me/evaluations/export")]
    public async Task<IActionResult> ExportMyEvaluations([FromBody] ExportPlayerHistoryRequest request)
    {
        CheckIsUserLoggedIn();
        request.PlayerId = JwtPayload.UserId;
        var result = await _exportService.ExportPlayerHistoryAsync(request, JwtPayload.UserId);
        return File(result.Data, result.ContentType, result.FileName);
    }

    [HttpGet("skill-matrices/{id:guid}/export")]
    public async Task<IActionResult> ExportSkillMatrix(
        [FromRoute] Guid id,
        [FromQuery] ExportFormat format = ExportFormat.Csv)
    {
        CheckIsUserLoggedIn();
        var result = await _exportService.ExportSkillMatrixAsync(id, format, JwtPayload.UserId);
        return File(result.Data, result.ContentType, result.FileName);
    }
}
