using Coaching.Application.DTOs.Evaluation;

namespace Coaching.Application.Interfaces.Services;

public interface IEvaluationGroupService
{
    Task<EvaluationGroupDto> CreateGroupAsync(Guid sessionId, CreateGroupDto dto, Guid userId);
    Task<EvaluationGroupDto> UpdateGroupAsync(Guid sessionId, Guid groupId, UpdateGroupDto dto, Guid userId);
    Task DeleteGroupAsync(Guid sessionId, Guid groupId, Guid userId);
    Task<IEnumerable<EvaluationGroupDto>> AutoSplitGroupsAsync(Guid sessionId, AutoSplitGroupsDto dto, Guid userId);
    Task<EvaluationGroupDto> AddPlayerToGroupAsync(Guid sessionId, Guid groupId, AssignPlayerToGroupDto dto, Guid userId);
    Task RemovePlayerFromGroupAsync(Guid sessionId, Guid groupId, Guid playerId, Guid userId);
    Task MovePlayerAsync(Guid sessionId, MovePlayerDto dto, Guid userId);
}
