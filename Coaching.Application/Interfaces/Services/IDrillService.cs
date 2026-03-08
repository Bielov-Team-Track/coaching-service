using Coaching.Application.DTOs.Drills;
using Shared.DTOs;

namespace Coaching.Application.Interfaces.Services;

public interface IDrillService
{
    // Drill CRUD
    Task<PagedResponse<DrillDto>> GetByFilterAsync(DrillFilterRequest filter, Guid? userId = null);
    Task<DrillDto?> GetByIdAsync(Guid id, Guid? userId = null);
    Task<DrillDto> CreateAsync(CreateDrillDto request, Guid userId);
    Task<DrillDto> UpdateAsync(UpdateDrillDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);

    // User's drills
    Task<IEnumerable<DrillDto>> GetCurrentUserDrillsAsync(Guid userId);

    // Club drills
    Task<IEnumerable<DrillDto>> GetClubDrillsAsync(Guid clubId, Guid userId);

    // Likes
    Task<DrillLikeStatusDto> LikeDrillAsync(Guid drillId, Guid userId);
    Task<DrillLikeStatusDto> UnlikeDrillAsync(Guid drillId, Guid userId);
    Task<DrillLikeStatusDto> GetLikeStatusAsync(Guid drillId, Guid userId);

    // Bookmarks
    Task<DrillBookmarkStatusDto> BookmarkDrillAsync(Guid drillId, Guid userId);
    Task<DrillBookmarkStatusDto> UnbookmarkDrillAsync(Guid drillId, Guid userId);
    Task<IEnumerable<BookmarkedDrillDto>> GetUserBookmarksAsync(Guid userId);

    // Comments
    Task<DrillCommentDto> CreateCommentAsync(Guid drillId, CreateDrillCommentDto request, Guid userId);
    Task<DrillCommentsResponseDto> GetCommentsAsync(Guid drillId, Guid? cursor, int limit);
    Task DeleteCommentAsync(Guid drillId, Guid commentId, Guid userId);

    // Attachments
    Task<DrillAttachmentUploadResponseDto> GetAttachmentUploadUrlAsync(Guid drillId, DrillAttachmentUploadRequestDto request, Guid userId);
    Task<DrillAttachmentDto> AddAttachmentAsync(Guid drillId, CreateDrillAttachmentDto request, Guid userId);
    Task DeleteAttachmentAsync(Guid drillId, Guid attachmentId, Guid userId);

    // Animations
    Task<DrillDto> UpdateAnimationsAsync(Guid drillId, UpdateDrillAnimationsDto request, Guid userId);
}
