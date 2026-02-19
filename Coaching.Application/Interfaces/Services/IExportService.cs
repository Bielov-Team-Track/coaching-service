using Coaching.Application.DTOs.Export;

namespace Coaching.Application.Interfaces.Services;

public interface IExportService
{
    Task<ExportResult> ExportEvaluationsAsync(ExportEvaluationRequest request, Guid requestingUserId);
    Task<ExportResult> ExportPlayerHistoryAsync(ExportPlayerHistoryRequest request, Guid requestingUserId);
    Task<ExportResult> ExportSkillMatrixAsync(Guid skillMatrixId, ExportFormat format, Guid requestingUserId);
}
