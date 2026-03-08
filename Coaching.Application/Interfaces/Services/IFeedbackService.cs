using Coaching.Application.DTOs.Feedback;

namespace Coaching.Application.Interfaces.Services;

public interface IFeedbackService
{
    Task<FeedbackDto> CreateAsync(CreateFeedbackDto request, Guid coachUserId);
    Task<FeedbackDto?> GetByIdAsync(Guid id, Guid requestingUserId);
    Task<FeedbackDto> UpdateAsync(Guid id, UpdateFeedbackDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
    Task<IEnumerable<FeedbackDto>> GetByEventIdAsync(Guid eventId, Guid requestingUserId);
    Task<FeedbackListResponseDto> GetReceivedFeedbackAsync(Guid userId, int page = 1, int pageSize = 20);
    Task<FeedbackListResponseDto> GetGivenFeedbackAsync(Guid userId, int page = 1, int pageSize = 20);
    Task<FeedbackDto> ShareWithPlayerAsync(Guid id, bool share, Guid userId);
    Task<FeedbackDto> AddImprovementPointAsync(Guid feedbackId, AddImprovementPointDto request, Guid userId);
    Task<FeedbackDto> UpdateImprovementPointAsync(Guid feedbackId, Guid pointId, UpdateImprovementPointDto request, Guid userId);
    Task<FeedbackDto> RemoveImprovementPointAsync(Guid feedbackId, Guid pointId, Guid userId);
    Task<FeedbackDto> AddDrillToPointAsync(Guid feedbackId, Guid pointId, Guid drillId, Guid userId);
    Task<FeedbackDto> RemoveDrillFromPointAsync(Guid feedbackId, Guid pointId, Guid drillId, Guid userId);
    Task<FeedbackDto> AddMediaToPointAsync(Guid feedbackId, Guid pointId, CreateImprovementPointMediaDto request, Guid userId);
    Task<FeedbackDto> RemoveMediaFromPointAsync(Guid feedbackId, Guid pointId, Guid mediaId, Guid userId);
    Task<FeedbackDto> AddPraiseAsync(Guid feedbackId, CreatePraiseDto request, Guid userId);
    Task<FeedbackDto> UpdatePraiseAsync(Guid feedbackId, UpdatePraiseDto request, Guid userId);
    Task<FeedbackDto> RemovePraiseAsync(Guid feedbackId, Guid userId);
}
