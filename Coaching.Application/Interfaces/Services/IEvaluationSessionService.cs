using Coaching.Application.DTOs.Evaluation;

namespace Coaching.Application.Interfaces.Services;

public interface IEvaluationSessionService
{
    Task<EvaluationSessionDto> CreateAsync(CreateEvaluationSessionDto request, Guid coachUserId);
    Task<EvaluationSessionDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<EvaluationSessionDto>> GetByClubIdAsync(Guid clubId, int page = 1, int pageSize = 20);
    Task<IEnumerable<EvaluationSessionDto>> GetMySessionsAsync(Guid coachUserId, int page = 1, int pageSize = 20);
    Task<EvaluationSessionDto> UpdateAsync(Guid id, UpdateEvaluationSessionDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);

    // Participants
    Task<EvaluationSessionDto> AddParticipantsAsync(Guid sessionId, AddParticipantsDto request, Guid userId);
    Task<EvaluationSessionDto> RemoveParticipantAsync(Guid sessionId, Guid participantId, Guid userId);
}
