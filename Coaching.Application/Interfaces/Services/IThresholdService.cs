using Coaching.Application.DTOs.Evaluation;

namespace Coaching.Application.Interfaces.Services;

public interface IThresholdService
{
    Task<EvaluationThresholdDto> CreateAsync(Guid clubId, CreateThresholdDto request, Guid userId);
    Task<IEnumerable<EvaluationThresholdDto>> GetByClubIdAsync(Guid clubId);
    Task<EvaluationThresholdDto> UpdateAsync(Guid id, UpdateThresholdDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
    Task<ThresholdCheckResult> CheckPlayerAsync(Guid clubId, PlayerEvaluationDto evaluation);
}
