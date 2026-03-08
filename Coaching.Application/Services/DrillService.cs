using System.Text.Json;
using AutoMapper;
using Coaching.Application.DTOs.Drills;
using Coaching.Application.Interfaces.Repositories;
using Coaching.Application.Interfaces.Services;
using Coaching.Domain.Enums;
using Coaching.Domain.Models.Drills;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Enums;
using Shared.Exceptions;
using Shared.DTOs;
using Shared.Options;
using Shared.Services.FileStorage.Intefaces;

namespace Coaching.Application.Services;

public class DrillService : IDrillService
{
    private readonly IDrillRepository _drillRepository;
    private readonly IDrillLikeRepository _likeRepository;
    private readonly IDrillBookmarkRepository _bookmarkRepository;
    private readonly IDrillCommentRepository _commentRepository;
    private readonly IDrillAttachmentRepository _attachmentRepository;
    private readonly IClubsGrpcClient _clubsClient;
    private readonly IFileService _fileService;
    private readonly S3Settings _s3Settings;
    private readonly IMapper _mapper;
    private readonly ILogger<DrillService> _logger;

    public DrillService(
        IDrillRepository drillRepository,
        IDrillLikeRepository likeRepository,
        IDrillBookmarkRepository bookmarkRepository,
        IDrillCommentRepository commentRepository,
        IDrillAttachmentRepository attachmentRepository,
        IClubsGrpcClient clubsClient,
        IFileService fileService,
        IOptions<S3Settings> s3Settings,
        IMapper mapper,
        ILogger<DrillService> logger)
    {
        _drillRepository = drillRepository;
        _likeRepository = likeRepository;
        _bookmarkRepository = bookmarkRepository;
        _commentRepository = commentRepository;
        _attachmentRepository = attachmentRepository;
        _clubsClient = clubsClient;
        _fileService = fileService;
        _s3Settings = s3Settings.Value;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PagedResponse<DrillDto>> GetByFilterAsync(DrillFilterRequest filter, Guid? userId = null)
    {
        var query = _drillRepository.Query();

        // Default to only public drills for general listing
        query = query.Where(d => d.Visibility == DrillVisibility.Public);

        if (filter.Category.HasValue)
            query = query.Where(d => d.Category == filter.Category.Value);

        if (filter.Intensity.HasValue)
            query = query.Where(d => d.Intensity == filter.Intensity.Value);

        if (filter.Skill.HasValue)
            query = query.Where(d => d.Skills.Contains(filter.Skill.Value));

        if (filter.CreatedByUserId.HasValue)
            query = query.Where(d => d.CreatedByUserId == filter.CreatedByUserId.Value);

        if (filter.ClubId.HasValue)
            query = query.Where(d => d.ClubId == filter.ClubId.Value);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var searchLower = filter.SearchTerm.ToLower();
            query = query.Where(d =>
                d.Name.ToLower().Contains(searchLower) ||
                (d.Description != null && d.Description.ToLower().Contains(searchLower)));
        }

        // Equipment filter - drills must have ALL specified equipment
        if (filter.Equipment != null && filter.Equipment.Length > 0)
        {
            var equipmentLower = filter.Equipment.Select(e => e.ToLower()).ToArray();
            foreach (var equipName in equipmentLower)
            {
                if (filter.RequiredEquipmentOnly == true)
                {
                    query = query.Where(d => d.Equipment.Any(e => !e.IsOptional && e.Name.ToLower().Contains(equipName)));
                }
                else
                {
                    query = query.Where(d => d.Equipment.Any(e => e.Name.ToLower().Contains(equipName)));
                }
            }
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply sorting
        var sortBy = filter.SortBy?.ToLower() ?? "likecount";
        var sortOrder = filter.SortOrder?.ToLower() ?? "desc";

        query = sortBy switch
        {
            "name" => sortOrder == "desc" ? query.OrderByDescending(d => d.Name) : query.OrderBy(d => d.Name),
            "createdat" => sortOrder == "desc" ? query.OrderByDescending(d => d.CreatedAt) : query.OrderBy(d => d.CreatedAt),
            "duration" => sortOrder == "desc" ? query.OrderByDescending(d => d.Duration) : query.OrderBy(d => d.Duration),
            _ => sortOrder == "desc" ? query.OrderByDescending(d => d.LikeCount) : query.OrderBy(d => d.LikeCount)
        };

        // Apply pagination
        var skip = (filter.Page - 1) * filter.Limit;
        query = query.Skip(skip).Take(filter.Limit);

        var drills = await query
            .Include(d => d.Attachments.OrderBy(a => a.Order))
            .Include(d => d.Equipment.OrderBy(e => e.Order))
            .Include(d => d.Creator)
            .ToListAsync();

        var dtos = _mapper.Map<IEnumerable<DrillDto>>(drills);
        await EnrichWithClubInfoAsync(dtos);
        await EnrichWithUserInteractionsAsync(dtos, userId);
        return PagedResponse<DrillDto>.Create(dtos, totalCount, filter.Page, filter.Limit);
    }

    public async Task<DrillDto?> GetByIdAsync(Guid id, Guid? userId = null)
    {
        var drill = await _drillRepository.GetByIdWithDetailsAsync(id);
        if (drill == null) return null;

        // Check visibility
        if (drill.Visibility == DrillVisibility.Private)
        {
            if (!userId.HasValue)
                throw new ForbiddenException("This drill is private");

            if (drill.CreatedByUserId != userId.Value)
            {
                throw new ForbiddenException("This drill is private");
            }
        }

        var dto = _mapper.Map<DrillDto>(drill);
        await EnrichWithClubInfoAsync([dto]);
        await EnrichWithUserInteractionsAsync([dto], userId);
        return dto;
    }

    public async Task<DrillDto> CreateAsync(CreateDrillDto request, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new BadRequestException("Name is required", ErrorCodeEnum.ValidationError);

        // Validate variation drill IDs exist
        var variationDrillIds = request.Variations?.Select(v => v.DrillId).Distinct().ToList() ?? [];
        if (variationDrillIds.Count > 0)
        {
            var existingDrills = await _drillRepository.Query()
                .Where(d => variationDrillIds.Contains(d.Id))
                .Select(d => d.Id)
                .ToListAsync();

            var missingIds = variationDrillIds.Except(existingDrills).ToList();
            if (missingIds.Count > 0)
                throw new BadRequestException($"Variation drill(s) not found: {string.Join(", ", missingIds)}", ErrorCodeEnum.EntityNotFound);
        }

        var drill = new Drill
        {
            Name = request.Name,
            Description = request.Description,
            Category = request.Category,
            Intensity = request.Intensity,
            Visibility = request.Visibility,
            Skills = request.Skills ?? [],
            Duration = request.Duration,
            MinPlayers = request.MinPlayers,
            MaxPlayers = request.MaxPlayers,
            Instructions = request.Instructions ?? [],
            CoachingPoints = request.CoachingPoints ?? [],
            VideoUrl = request.VideoUrl,
            CreatedByUserId = userId,
            ClubId = request.ClubId,
            LikeCount = 0
        };

        // Add equipment
        if (request.Equipment != null)
        {
            for (int i = 0; i < request.Equipment.Length; i++)
            {
                var equipmentInput = request.Equipment[i];
                drill.Equipment.Add(new DrillEquipment
                {
                    DrillId = drill.Id,
                    Name = equipmentInput.Name,
                    IsOptional = equipmentInput.IsOptional,
                    Order = i
                });
            }
        }

        // Add variations
        if (request.Variations != null)
        {
            for (int i = 0; i < request.Variations.Length; i++)
            {
                var variationInput = request.Variations[i];
                drill.Variations.Add(new DrillVariation
                {
                    SourceDrillId = drill.Id,
                    TargetDrillId = variationInput.DrillId,
                    Note = variationInput.Note,
                    Order = i
                });
            }
        }

        _drillRepository.Add(drill);
        await _drillRepository.SaveChangesAsync();

        // Re-fetch with details for proper mapping
        var createdDrill = await _drillRepository.GetByIdWithDetailsAsync(drill.Id);
        var dto = _mapper.Map<DrillDto>(createdDrill);
        await EnrichWithClubInfoAsync([dto]);
        return dto;
    }

    public async Task<DrillDto> UpdateAsync(UpdateDrillDto request, Guid userId)
    {
        var drill = await _drillRepository.GetByIdWithDetailsAsync(request.Id);
        if (drill == null)
            throw new EntityNotFoundException("Drill not found");

        if (!CanModifyDrill(drill, userId))
            throw new ForbiddenException("Only the creator can update this drill");

        // Validate variation drill IDs exist
        var variationDrillIds = request.Variations?.Select(v => v.DrillId).Distinct().ToList() ?? [];
        if (variationDrillIds.Count > 0)
        {
            // Prevent self-referencing
            if (variationDrillIds.Contains(request.Id))
                throw new BadRequestException("A drill cannot be a variation of itself", ErrorCodeEnum.ValidationError);

            var existingDrills = await _drillRepository.Query()
                .Where(d => variationDrillIds.Contains(d.Id))
                .Select(d => d.Id)
                .ToListAsync();

            var missingIds = variationDrillIds.Except(existingDrills).ToList();
            if (missingIds.Count > 0)
                throw new BadRequestException($"Variation drill(s) not found: {string.Join(", ", missingIds)}", ErrorCodeEnum.EntityNotFound);
        }

        drill.Name = request.Name;
        drill.Description = request.Description;
        drill.Category = request.Category;
        drill.Intensity = request.Intensity;
        drill.Visibility = request.Visibility;
        drill.Skills = request.Skills ?? [];
        drill.Duration = request.Duration;
        drill.MinPlayers = request.MinPlayers;
        drill.MaxPlayers = request.MaxPlayers;
        drill.Instructions = request.Instructions ?? [];
        drill.CoachingPoints = request.CoachingPoints ?? [];
        drill.VideoUrl = request.VideoUrl;
        drill.ClubId = request.ClubId;
        drill.UpdatedAt = DateTime.UtcNow;

        // Update equipment - clear existing and add new
        drill.Equipment.Clear();
        if (request.Equipment != null)
        {
            for (int i = 0; i < request.Equipment.Length; i++)
            {
                var equipmentInput = request.Equipment[i];
                drill.Equipment.Add(new DrillEquipment
                {
                    DrillId = drill.Id,
                    Name = equipmentInput.Name,
                    IsOptional = equipmentInput.IsOptional,
                    Order = i
                });
            }
        }

        // Update variations - clear existing and add new
        drill.Variations.Clear();
        if (request.Variations != null)
        {
            for (int i = 0; i < request.Variations.Length; i++)
            {
                var variationInput = request.Variations[i];
                drill.Variations.Add(new DrillVariation
                {
                    SourceDrillId = drill.Id,
                    TargetDrillId = variationInput.DrillId,
                    Note = variationInput.Note,
                    Order = i
                });
            }
        }

        _drillRepository.Update(drill);
        await _drillRepository.SaveChangesAsync();

        // Re-fetch with details for proper mapping
        var updatedDrill = await _drillRepository.GetByIdWithDetailsAsync(drill.Id);
        var dto = _mapper.Map<DrillDto>(updatedDrill);
        await EnrichWithClubInfoAsync([dto]);
        return dto;
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var drill = await _drillRepository.GetByIdAsync(id);
        if (drill == null)
            throw new EntityNotFoundException("Drill not found");

        if (!CanModifyDrill(drill, userId))
            throw new ForbiddenException("Only the creator can delete this drill");

        _drillRepository.Delete(drill);
        await _drillRepository.SaveChangesAsync();
    }

    public async Task<IEnumerable<DrillDto>> GetCurrentUserDrillsAsync(Guid userId)
    {
        var drills = await _drillRepository.GetByCreatorAsync(userId);
        var dtos = _mapper.Map<IEnumerable<DrillDto>>(drills);
        await EnrichWithClubInfoAsync(dtos);
        await EnrichWithUserInteractionsAsync(dtos, userId);
        return dtos;
    }

    public async Task<IEnumerable<DrillDto>> GetClubDrillsAsync(Guid clubId, Guid userId)
    {
        var drills = await _drillRepository.GetByClubAsync(clubId);
        var dtos = _mapper.Map<IEnumerable<DrillDto>>(drills);
        await EnrichWithClubInfoAsync(dtos);
        await EnrichWithUserInteractionsAsync(dtos, userId);
        return dtos;
    }

    // =========================================================================
    // LIKES
    // =========================================================================

    public async Task<DrillLikeStatusDto> LikeDrillAsync(Guid drillId, Guid userId)
    {
        var drill = await _drillRepository.GetByIdAsync(drillId);
        if (drill == null)
            throw new EntityNotFoundException("Drill not found");

        var existingLike = await _likeRepository.GetByDrillAndUserAsync(drillId, userId);
        if (existingLike != null)
        {
            return new DrillLikeStatusDto
            {
                IsLiked = true,
                LikeCount = drill.LikeCount
            };
        }

        var like = new DrillLike
        {
            DrillId = drillId,
            UserId = userId
        };

        _likeRepository.Add(like);

        drill.LikeCount++;
        _drillRepository.Update(drill);

        await _drillRepository.SaveChangesAsync();

        return new DrillLikeStatusDto
        {
            IsLiked = true,
            LikeCount = drill.LikeCount
        };
    }

    public async Task<DrillLikeStatusDto> UnlikeDrillAsync(Guid drillId, Guid userId)
    {
        var drill = await _drillRepository.GetByIdAsync(drillId);
        if (drill == null)
            throw new EntityNotFoundException("Drill not found");

        var existingLike = await _likeRepository.GetByDrillAndUserAsync(drillId, userId);
        if (existingLike == null)
        {
            return new DrillLikeStatusDto
            {
                IsLiked = false,
                LikeCount = drill.LikeCount
            };
        }

        _likeRepository.Delete(existingLike);

        drill.LikeCount = Math.Max(0, drill.LikeCount - 1);
        _drillRepository.Update(drill);

        await _drillRepository.SaveChangesAsync();

        return new DrillLikeStatusDto
        {
            IsLiked = false,
            LikeCount = drill.LikeCount
        };
    }

    public async Task<DrillLikeStatusDto> GetLikeStatusAsync(Guid drillId, Guid userId)
    {
        var drill = await _drillRepository.GetByIdAsync(drillId);
        if (drill == null)
            throw new EntityNotFoundException("Drill not found");

        var existingLike = await _likeRepository.GetByDrillAndUserAsync(drillId, userId);

        return new DrillLikeStatusDto
        {
            IsLiked = existingLike != null,
            LikeCount = drill.LikeCount
        };
    }

    // =========================================================================
    // BOOKMARKS
    // =========================================================================

    public async Task<DrillBookmarkStatusDto> BookmarkDrillAsync(Guid drillId, Guid userId)
    {
        var drill = await _drillRepository.GetByIdAsync(drillId);
        if (drill == null)
            throw new EntityNotFoundException("Drill not found");

        var existingBookmark = await _bookmarkRepository.GetByDrillAndUserAsync(drillId, userId);
        if (existingBookmark != null)
        {
            return new DrillBookmarkStatusDto { IsBookmarked = true };
        }

        var bookmark = new DrillBookmark
        {
            DrillId = drillId,
            UserId = userId
        };

        _bookmarkRepository.Add(bookmark);
        await _bookmarkRepository.SaveChangesAsync();

        return new DrillBookmarkStatusDto { IsBookmarked = true };
    }

    public async Task<DrillBookmarkStatusDto> UnbookmarkDrillAsync(Guid drillId, Guid userId)
    {
        var existingBookmark = await _bookmarkRepository.GetByDrillAndUserAsync(drillId, userId);
        if (existingBookmark == null)
        {
            return new DrillBookmarkStatusDto { IsBookmarked = false };
        }

        _bookmarkRepository.Delete(existingBookmark);
        await _bookmarkRepository.SaveChangesAsync();

        return new DrillBookmarkStatusDto { IsBookmarked = false };
    }

    public async Task<IEnumerable<BookmarkedDrillDto>> GetUserBookmarksAsync(Guid userId)
    {
        var bookmarks = await _bookmarkRepository.GetByUserAsync(userId);

        return bookmarks.Select(b => new BookmarkedDrillDto
        {
            Id = b.Drill.Id,
            Name = b.Drill.Name,
            Category = b.Drill.Category,
            Intensity = b.Drill.Intensity,
            LikeCount = b.Drill.LikeCount,
            BookmarkedAt = b.CreatedAt ?? DateTime.UtcNow
        });
    }

    // =========================================================================
    // COMMENTS
    // =========================================================================

    public async Task<DrillCommentDto> CreateCommentAsync(Guid drillId, CreateDrillCommentDto request, Guid userId)
    {
        var drill = await _drillRepository.GetByIdAsync(drillId);
        if (drill == null)
            throw new EntityNotFoundException("Drill not found");

        if (string.IsNullOrWhiteSpace(request.Content))
            throw new BadRequestException("Comment content is required", ErrorCodeEnum.ValidationError);

        if (request.ParentCommentId.HasValue)
        {
            var parentComment = await _commentRepository.GetByIdAsync(request.ParentCommentId.Value);
            if (parentComment == null || parentComment.DrillId != drillId)
                throw new BadRequestException("Parent comment not found", ErrorCodeEnum.EntityNotFound);
        }

        var comment = new DrillComment
        {
            DrillId = drillId,
            UserId = userId,
            Content = request.Content,
            ParentCommentId = request.ParentCommentId
        };

        _commentRepository.Add(comment);
        await _commentRepository.SaveChangesAsync();

        var createdComment = await _commentRepository.GetByIdWithDetailsAsync(comment.Id);
        return _mapper.Map<DrillCommentDto>(createdComment);
    }

    public async Task<DrillCommentsResponseDto> GetCommentsAsync(Guid drillId, Guid? cursor, int limit)
    {
        var drill = await _drillRepository.GetByIdAsync(drillId);
        if (drill == null)
            throw new EntityNotFoundException("Drill not found");

        var comments = (await _commentRepository.GetByDrillWithCursorAsync(drillId, cursor, limit)).ToList();
        var hasMore = comments.Count > limit;

        if (hasMore)
        {
            comments = comments.Take(limit).ToList();
        }

        var nextCursor = hasMore && comments.Count > 0 ? comments.Last().Id : (Guid?)null;

        return new DrillCommentsResponseDto
        {
            Items = _mapper.Map<ICollection<DrillCommentDto>>(comments),
            NextCursor = nextCursor,
            HasMore = hasMore
        };
    }

    public async Task DeleteCommentAsync(Guid drillId, Guid commentId, Guid userId)
    {
        var comment = await _commentRepository.GetByIdAsync(commentId);
        if (comment == null || comment.DrillId != drillId)
            throw new EntityNotFoundException("Comment not found");

        if (comment.UserId != userId)
            throw new ForbiddenException("Only the comment author can delete this comment");

        comment.IsDeleted = true;
        _commentRepository.Update(comment);
        await _commentRepository.SaveChangesAsync();
    }

    // =========================================================================
    // ATTACHMENTS
    // =========================================================================

    public async Task<DrillAttachmentUploadResponseDto> GetAttachmentUploadUrlAsync(
        Guid drillId, DrillAttachmentUploadRequestDto request, Guid userId)
    {
        var drill = await _drillRepository.GetByIdAsync(drillId);
        if (drill == null)
            throw new EntityNotFoundException("Drill not found");

        if (!CanModifyDrill(drill, userId))
            throw new ForbiddenException("Only the creator can add attachments");

        var fileId = Guid.NewGuid();
        var extension = Path.GetExtension(request.FileName);
        var s3Key = $"drills/{drillId}/{fileId}{extension}";

        var uploadUrl = await _fileService.GetPresignedUploadLink(s3Key, _s3Settings.Bucket, request.ContentType);
        var fileUrl = _fileService.GetPublicUrl(s3Key);

        return new DrillAttachmentUploadResponseDto
        {
            UploadUrl = uploadUrl,
            FileUrl = fileUrl
        };
    }

    public async Task<DrillAttachmentDto> AddAttachmentAsync(Guid drillId, CreateDrillAttachmentDto request, Guid userId)
    {
        var drill = await _drillRepository.GetByIdAsync(drillId);
        if (drill == null)
            throw new EntityNotFoundException("Drill not found");

        if (!CanModifyDrill(drill, userId))
            throw new ForbiddenException("Only the creator can add attachments");

        var maxOrder = await _attachmentRepository.GetMaxOrderForDrillAsync(drillId);

        var attachment = new DrillAttachment
        {
            DrillId = drillId,
            FileName = request.FileName,
            FileUrl = request.FileUrl,
            FileType = request.FileType,
            FileSize = request.FileSize,
            Order = maxOrder + 1
        };

        _attachmentRepository.Add(attachment);
        await _attachmentRepository.SaveChangesAsync();

        return _mapper.Map<DrillAttachmentDto>(attachment);
    }

    public async Task DeleteAttachmentAsync(Guid drillId, Guid attachmentId, Guid userId)
    {
        var drill = await _drillRepository.GetByIdAsync(drillId);
        if (drill == null)
            throw new EntityNotFoundException("Drill not found");

        if (!CanModifyDrill(drill, userId))
            throw new ForbiddenException("Only the creator can delete attachments");

        var attachment = await _attachmentRepository.GetByIdAsync(attachmentId);
        if (attachment == null || attachment.DrillId != drillId)
            throw new EntityNotFoundException("Attachment not found");

        _attachmentRepository.Delete(attachment);
        await _attachmentRepository.SaveChangesAsync();
    }

    // =========================================================================
    // ANIMATION
    // =========================================================================

    public async Task<DrillDto> UpdateAnimationsAsync(Guid drillId, UpdateDrillAnimationsDto request, Guid userId)
    {
        var drill = await _drillRepository.GetByIdWithDetailsAsync(drillId);
        if (drill == null)
            throw new EntityNotFoundException("Drill not found");

        if (!CanModifyDrill(drill, userId))
            throw new ForbiddenException("Only the creator can update the animations");

        drill.Animations = request.Animations.Count > 0
            ? JsonSerializer.Serialize(request.Animations, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
            : null;
        drill.UpdatedAt = DateTime.UtcNow;

        _drillRepository.Update(drill);
        await _drillRepository.SaveChangesAsync();

        var updatedDrill = await _drillRepository.GetByIdWithDetailsAsync(drill.Id);
        var dto = _mapper.Map<DrillDto>(updatedDrill);
        await EnrichWithClubInfoAsync([dto]);
        return dto;
    }

    private bool CanModifyDrill(Drill drill, Guid userId)
    {
        return drill.CreatedByUserId == userId;
    }

    private async Task EnrichWithUserInteractionsAsync(IEnumerable<DrillDto> drills, Guid? userId)
    {
        var drillList = drills.ToList();
        if (drillList.Count == 0) return;

        var drillIds = drillList.Select(d => d.Id).ToList();

        var bookmarkCounts = await _bookmarkRepository.GetBookmarkCountsAsync(drillIds);
        foreach (var drill in drillList)
        {
            drill.BookmarkCount = bookmarkCounts.TryGetValue(drill.Id, out var count) ? count : 0;
        }

        if (!userId.HasValue) return;

        var userLikedDrillIds = await _likeRepository.GetUserLikedDrillIdsAsync(userId.Value, drillIds);
        var likedSet = userLikedDrillIds.ToHashSet();

        var userBookmarkedDrillIds = await _bookmarkRepository.GetUserBookmarkedDrillIdsAsync(userId.Value, drillIds);
        var bookmarkedSet = userBookmarkedDrillIds.ToHashSet();

        foreach (var drill in drillList)
        {
            drill.IsLiked = likedSet.Contains(drill.Id);
            drill.IsBookmarked = bookmarkedSet.Contains(drill.Id);
        }
    }

    private async Task EnrichWithClubInfoAsync(IEnumerable<DrillDto> drills)
    {
        var drillList = drills.ToList();
        if (drillList.Count == 0) return;

        var clubIds = drillList
            .Where(d => d.ClubId.HasValue)
            .Select(d => d.ClubId!.Value)
            .Distinct()
            .ToList();

        if (clubIds.Count == 0) return;

        try
        {
            var clubInfos = await _clubsClient.GetClubInfoAsync(clubIds);

            foreach (var drill in drillList)
            {
                if (drill.ClubId.HasValue && clubInfos.TryGetValue(drill.ClubId.Value, out var clubInfo))
                {
                    drill.ClubName = clubInfo.Name;
                    drill.ClubLogoUrl = clubInfo.LogoUrl;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to enrich drills with club info");
        }
    }
}
