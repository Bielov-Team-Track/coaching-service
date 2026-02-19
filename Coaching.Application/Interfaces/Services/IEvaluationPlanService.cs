using Coaching.Application.DTOs.Evaluation;

namespace Coaching.Application.Interfaces.Services;

public interface IEvaluationPlanService
{
    Task<EvaluationPlanDto> CreateAsync(CreateEvaluationPlanDto request, Guid userId);
    Task<EvaluationPlanDto?> GetByIdAsync(Guid id);
    Task<List<EvaluationPlanDto>> GetByClubIdAsync(Guid clubId);
    Task<List<EvaluationPlanDto>> GetByUserIdAsync(Guid userId);
    Task<EvaluationPlanDto> UpdateAsync(Guid id, UpdateEvaluationPlanDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);

    // Items
    Task<EvaluationPlanDto> AddItemAsync(Guid planId, AddPlanItemDto request, Guid userId);
    Task<EvaluationPlanDto> RemoveItemAsync(Guid planId, Guid itemId, Guid userId);
    Task<EvaluationPlanDto> ReorderItemsAsync(Guid planId, List<Guid> itemIds, Guid userId);
}
