using Coaching.Application.DTOs.Evaluation;

namespace Coaching.Application.Interfaces.Services;

public interface IPlayerEvaluationService
{
    Task<PlayerEvaluationDto> CreateAsync(Guid sessionId, CreatePlayerEvaluationDto request, Guid coachUserId);
    Task<PlayerEvaluationDto?> GetByIdAsync(Guid id, Guid requestingUserId);
    Task<EvaluationSummaryDto> GetSessionSummaryAsync(Guid sessionId, Guid requestingUserId);
    Task<IEnumerable<PlayerEvaluationDto>> GetPlayerHistoryAsync(Guid playerId, int page = 1, int pageSize = 20);

    Task<PlayerEvaluationDto> RecordMetricScoresAsync(Guid evaluationId, RecordMetricScoresDto request, Guid userId);
    Task<PlayerEvaluationDto> UpdateOutcomeAsync(Guid evaluationId, UpdateEvaluationOutcomeDto request, Guid userId);
    Task<PlayerEvaluationDto> ShareWithPlayerAsync(Guid evaluationId, bool share, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
