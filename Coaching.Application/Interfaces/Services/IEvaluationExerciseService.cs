using Coaching.Application.DTOs.Evaluation;

namespace Coaching.Application.Interfaces.Services;

public interface IEvaluationExerciseService
{
    Task<EvaluationExerciseDto> CreateAsync(CreateEvaluationExerciseDto request, Guid userId);
    Task<EvaluationExerciseDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<EvaluationExerciseDto>> GetByClubIdAsync(Guid clubId);
    Task<ExerciseListResponseDto> GetPublicExercisesAsync(int page = 1, int pageSize = 20);
    Task<EvaluationExerciseDto> UpdateAsync(Guid id, UpdateEvaluationExerciseDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);

    // Metrics
    Task<EvaluationExerciseDto> AddMetricAsync(Guid exerciseId, AddMetricDto request, Guid userId);
    Task<EvaluationExerciseDto> UpdateMetricAsync(Guid exerciseId, Guid metricId, UpdateEvaluationMetricDto request, Guid userId);
    Task<EvaluationExerciseDto> RemoveMetricAsync(Guid exerciseId, Guid metricId, Guid userId);
    Task<EvaluationExerciseDto> UpdateMetricSkillWeightsAsync(Guid exerciseId, Guid metricId, List<CreateMetricSkillWeightDto> weights, Guid userId);
}
