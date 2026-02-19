using AutoMapper;
using Coaching.Application.DTOs.Feedback;
using Coaching.Application.Interfaces.Repositories;
using Coaching.Application.Interfaces.Services;
using Coaching.Domain.Models.Feedback;
using Ganss.Xss;
using Microsoft.EntityFrameworkCore;
using Shared.DataAccess.Repositories.Interfaces;
using Shared.Enums;
using Shared.Exceptions;

namespace Coaching.Application.Services;

public class FeedbackService(
    IFeedbackRepository feedbackRepository,
    IRepository<ImprovementPoint> pointRepository,
    IRepository<ImprovementPointDrill> drillLinkRepository,
    IRepository<ImprovementPointMedia> mediaRepository,
    IRepository<Praise> praiseRepository,
    IRepository<Coaching.Domain.Models.Drills.Drill> drillRepository,
    IFeedbackAuthorizationService authorizationService,
    IMapper mapper) : IFeedbackService
{
    private static readonly HtmlSanitizer _htmlSanitizer = CreateSanitizer();

    private static HtmlSanitizer CreateSanitizer()
    {
        var sanitizer = new HtmlSanitizer();
        sanitizer.AllowedTags.Clear();
        sanitizer.AllowedTags.UnionWith(new[]
        {
            "p", "br", "strong", "em", "u", "s", "a",
            "ul", "ol", "li", "h1", "h2", "h3",
            "blockquote", "code", "pre"
        });
        sanitizer.AllowedAttributes.Clear();
        sanitizer.AllowedAttributes.UnionWith(new[] { "href", "target", "rel" });
        sanitizer.AllowedSchemes.Clear();
        sanitizer.AllowedSchemes.UnionWith(new[] { "http", "https" });
        return sanitizer;
    }

    public async Task<FeedbackDto> CreateAsync(CreateFeedbackDto request, Guid coachUserId)
    {
        // Validate authorization before creating.
        // ValidateCreateAsync returns the resolved ClubId from event context (if event-linked)
        // so we don't need to call GetEventContextAsync again.
        var resolvedClubId = await authorizationService.ValidateCreateAsync(request, coachUserId);

        // For event-linked feedback, set ClubId from the resolved event context automatically
        if (resolvedClubId.HasValue)
            request = request with { ClubId = resolvedClubId.Value };

        var feedback = mapper.Map<Feedback>(request);
        feedback.CoachUserId = coachUserId;

        if (!string.IsNullOrEmpty(feedback.Content))
        {
            // Gate 1: Raw input size check BEFORE sanitization
            if (feedback.Content.Length > 100_000)
                throw new BadRequestException("Feedback content is too large", ErrorCodeEnum.ValidationError);

            // Sanitize HTML to prevent stored XSS
            feedback.Content = _htmlSanitizer.Sanitize(feedback.Content);

            // Gate 2: Post-sanitization length check
            if (feedback.Content.Length > 50_000)
                throw new BadRequestException("Feedback content exceeds maximum length of 50,000 characters", ErrorCodeEnum.ValidationError);

            feedback.ContentPlainText = StripHtml(feedback.Content);
            // Phase A: Keep Comment in sync for backward compat / rollback
            feedback.Comment = feedback.ContentPlainText;
        }

        feedbackRepository.Add(feedback);
        await feedbackRepository.SaveChangesAsync();

        if (request.ImprovementPoints != null)
        {
            var order = 1;
            foreach (var pointDto in request.ImprovementPoints)
            {
                await AddImprovementPointInternal(feedback.Id, pointDto, order++);
            }
        }

        if (request.Praise != null)
        {
            var praise = mapper.Map<Praise>(request.Praise);
            praise.FeedbackId = feedback.Id;
            praiseRepository.Add(praise);
            await praiseRepository.SaveChangesAsync();
        }

        return await GetByIdAsync(feedback.Id, coachUserId) ?? throw new Exception("Failed to retrieve created feedback");
    }

    public async Task<FeedbackDto?> GetByIdAsync(Guid id, Guid requestingUserId)
    {
        var feedback = await feedbackRepository.GetByIdWithDetailsAsync(id);
        if (feedback == null) return null;

        if (feedback.CoachUserId != requestingUserId &&
            (feedback.RecipientUserId != requestingUserId || !feedback.SharedWithPlayer))
        {
            return null;
        }

        return mapper.Map<FeedbackDto>(feedback);
    }

    public async Task<FeedbackDto> UpdateAsync(Guid id, UpdateFeedbackDto request, Guid userId)
    {
        var feedback = await feedbackRepository.GetByIdAsync(id);
        if (feedback == null)
            throw new EntityNotFoundException("Feedback not found");

        if (feedback.CoachUserId != userId)
            throw new ForbiddenException("Only the coach can update this feedback");

        // Handle content update from either Content or Comment field (Phase A compat)
        var newContent = request.Content ?? request.Comment;
        if (newContent != null)
        {
            // Gate 1: Raw input size check before sanitization
            if (newContent.Length > 100_000)
                throw new BadRequestException("Feedback content is too large", ErrorCodeEnum.ValidationError);

            feedback.Content = _htmlSanitizer.Sanitize(newContent);

            // Gate 2: Post-sanitization length check
            if (feedback.Content.Length > 50_000)
                throw new BadRequestException("Feedback content exceeds maximum length of 50,000 characters", ErrorCodeEnum.ValidationError);

            feedback.ContentPlainText = StripHtml(feedback.Content);
            // Phase A: Keep Comment in sync
            feedback.Comment = feedback.ContentPlainText;
        }

        if (request.SharedWithPlayer.HasValue) feedback.SharedWithPlayer = request.SharedWithPlayer.Value;

        feedbackRepository.Update(feedback);
        await feedbackRepository.SaveChangesAsync();

        return await GetByIdAsync(id, userId) ?? throw new Exception("Failed to retrieve feedback");
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var feedback = await feedbackRepository.GetByIdAsync(id);
        if (feedback == null)
            throw new EntityNotFoundException("Feedback not found");

        if (feedback.CoachUserId != userId)
            throw new ForbiddenException("Only the coach can delete this feedback");

        feedback.IsDeleted = true;
        feedbackRepository.Update(feedback);
        await feedbackRepository.SaveChangesAsync();
    }

    public async Task<IEnumerable<FeedbackDto>> GetByEventIdAsync(Guid eventId, Guid requestingUserId)
    {
        var feedbacks = await feedbackRepository.GetByEventIdAsync(eventId);

        var accessible = feedbacks.Where(f =>
            f.CoachUserId == requestingUserId ||
            (f.RecipientUserId == requestingUserId && f.SharedWithPlayer));

        return mapper.Map<IEnumerable<FeedbackDto>>(accessible);
    }

    public async Task<FeedbackListResponseDto> GetReceivedFeedbackAsync(Guid userId, int page = 1, int pageSize = 20)
    {
        var feedbacks = await feedbackRepository.GetByRecipientIdAsync(userId, page, pageSize);
        var total = await feedbackRepository.Query()
            .CountAsync(f => f.RecipientUserId == userId && f.SharedWithPlayer && !f.IsDeleted);

        return new FeedbackListResponseDto
        {
            Items = mapper.Map<IEnumerable<FeedbackDto>>(feedbacks),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<FeedbackListResponseDto> GetGivenFeedbackAsync(Guid userId, int page = 1, int pageSize = 20)
    {
        var feedbacks = await feedbackRepository.GetByCoachIdAsync(userId, page, pageSize);
        var total = await feedbackRepository.Query()
            .CountAsync(f => f.CoachUserId == userId && !f.IsDeleted);

        return new FeedbackListResponseDto
        {
            Items = mapper.Map<IEnumerable<FeedbackDto>>(feedbacks),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<FeedbackDto> ShareWithPlayerAsync(Guid id, bool share, Guid userId)
    {
        var feedback = await feedbackRepository.GetByIdAsync(id);
        if (feedback == null)
            throw new EntityNotFoundException("Feedback not found");

        if (feedback.CoachUserId != userId)
            throw new ForbiddenException("Only the coach can share this feedback");

        feedback.SharedWithPlayer = share;
        feedbackRepository.Update(feedback);
        await feedbackRepository.SaveChangesAsync();

        return await GetByIdAsync(id, userId) ?? throw new Exception("Failed to retrieve feedback");
    }

    public async Task<FeedbackDto> AddImprovementPointAsync(Guid feedbackId, AddImprovementPointDto request, Guid userId)
    {
        var feedback = await feedbackRepository.GetByIdWithDetailsAsync(feedbackId);
        if (feedback == null)
            throw new EntityNotFoundException("Feedback not found");

        if (feedback.CoachUserId != userId)
            throw new ForbiddenException("Only the coach can modify this feedback");

        var maxOrder = feedback.ImprovementPoints.Any() ? feedback.ImprovementPoints.Max(p => p.Order) : 0;
        var order = request.Order ?? maxOrder + 1;

        await AddImprovementPointInternal(feedbackId, new CreateImprovementPointDto
        {
            Description = request.Description,
            DrillIds = request.DrillIds,
            MediaLinks = request.MediaLinks
        }, order);

        return await GetByIdAsync(feedbackId, userId) ?? throw new Exception("Failed to retrieve feedback");
    }

    public async Task<FeedbackDto> UpdateImprovementPointAsync(Guid feedbackId, Guid pointId, UpdateImprovementPointDto request, Guid userId)
    {
        var feedback = await feedbackRepository.GetByIdAsync(feedbackId);
        if (feedback == null)
            throw new EntityNotFoundException("Feedback not found");

        if (feedback.CoachUserId != userId)
            throw new ForbiddenException("Only the coach can modify this feedback");

        var point = await pointRepository.GetByIdAsync(pointId);
        if (point == null || point.FeedbackId != feedbackId)
            throw new EntityNotFoundException("Improvement point not found");

        if (request.Description != null) point.Description = request.Description;

        pointRepository.Update(point);
        await pointRepository.SaveChangesAsync();

        return await GetByIdAsync(feedbackId, userId) ?? throw new Exception("Failed to retrieve feedback");
    }

    public async Task<FeedbackDto> RemoveImprovementPointAsync(Guid feedbackId, Guid pointId, Guid userId)
    {
        var feedback = await feedbackRepository.GetByIdAsync(feedbackId);
        if (feedback == null)
            throw new EntityNotFoundException("Feedback not found");

        if (feedback.CoachUserId != userId)
            throw new ForbiddenException("Only the coach can modify this feedback");

        var point = await pointRepository.GetByIdAsync(pointId);
        if (point == null || point.FeedbackId != feedbackId)
            throw new EntityNotFoundException("Improvement point not found");

        point.IsDeleted = true;
        pointRepository.Update(point);
        await pointRepository.SaveChangesAsync();

        return await GetByIdAsync(feedbackId, userId) ?? throw new Exception("Failed to retrieve feedback");
    }

    public async Task<FeedbackDto> AddDrillToPointAsync(Guid feedbackId, Guid pointId, Guid drillId, Guid userId)
    {
        var feedback = await feedbackRepository.GetByIdAsync(feedbackId);
        if (feedback == null)
            throw new EntityNotFoundException("Feedback not found");

        if (feedback.CoachUserId != userId)
            throw new ForbiddenException("Only the coach can modify this feedback");

        // Validate drill exists locally (both in coaching-service now)
        var drill = await drillRepository.GetByIdAsync(drillId);
        if (drill == null)
            throw new EntityNotFoundException("Drill not found");

        var link = new ImprovementPointDrill
        {
            ImprovementPointId = pointId,
            DrillId = drillId
        };

        drillLinkRepository.Add(link);
        await drillLinkRepository.SaveChangesAsync();

        return await GetByIdAsync(feedbackId, userId) ?? throw new Exception("Failed to retrieve feedback");
    }

    public async Task<FeedbackDto> RemoveDrillFromPointAsync(Guid feedbackId, Guid pointId, Guid drillId, Guid userId)
    {
        var feedback = await feedbackRepository.GetByIdAsync(feedbackId);
        if (feedback == null)
            throw new EntityNotFoundException("Feedback not found");

        if (feedback.CoachUserId != userId)
            throw new ForbiddenException("Only the coach can modify this feedback");

        var link = await drillLinkRepository.Query()
            .FirstOrDefaultAsync(l => l.ImprovementPointId == pointId && l.DrillId == drillId);

        if (link != null)
        {
            link.IsDeleted = true;
            drillLinkRepository.Update(link);
            await drillLinkRepository.SaveChangesAsync();
        }

        return await GetByIdAsync(feedbackId, userId) ?? throw new Exception("Failed to retrieve feedback");
    }

    public async Task<FeedbackDto> AddMediaToPointAsync(Guid feedbackId, Guid pointId, CreateImprovementPointMediaDto request, Guid userId)
    {
        var feedback = await feedbackRepository.GetByIdAsync(feedbackId);
        if (feedback == null)
            throw new EntityNotFoundException("Feedback not found");

        if (feedback.CoachUserId != userId)
            throw new ForbiddenException("Only the coach can modify this feedback");

        var media = mapper.Map<ImprovementPointMedia>(request);
        media.ImprovementPointId = pointId;

        mediaRepository.Add(media);
        await mediaRepository.SaveChangesAsync();

        return await GetByIdAsync(feedbackId, userId) ?? throw new Exception("Failed to retrieve feedback");
    }

    public async Task<FeedbackDto> RemoveMediaFromPointAsync(Guid feedbackId, Guid pointId, Guid mediaId, Guid userId)
    {
        var feedback = await feedbackRepository.GetByIdAsync(feedbackId);
        if (feedback == null)
            throw new EntityNotFoundException("Feedback not found");

        if (feedback.CoachUserId != userId)
            throw new ForbiddenException("Only the coach can modify this feedback");

        var media = await mediaRepository.GetByIdAsync(mediaId);
        if (media == null || media.ImprovementPointId != pointId)
            throw new EntityNotFoundException("Media not found");

        media.IsDeleted = true;
        mediaRepository.Update(media);
        await mediaRepository.SaveChangesAsync();

        return await GetByIdAsync(feedbackId, userId) ?? throw new Exception("Failed to retrieve feedback");
    }

    public async Task<FeedbackDto> AddPraiseAsync(Guid feedbackId, CreatePraiseDto request, Guid userId)
    {
        var feedback = await feedbackRepository.GetByIdWithDetailsAsync(feedbackId);
        if (feedback == null)
            throw new EntityNotFoundException("Feedback not found");

        if (feedback.CoachUserId != userId)
            throw new ForbiddenException("Only the coach can modify this feedback");

        if (feedback.Praise != null)
            throw new ConflictException("Feedback already has praise. Update or remove it first.");

        var praise = mapper.Map<Praise>(request);
        praise.FeedbackId = feedbackId;

        praiseRepository.Add(praise);
        await praiseRepository.SaveChangesAsync();

        return await GetByIdAsync(feedbackId, userId) ?? throw new Exception("Failed to retrieve feedback");
    }

    public async Task<FeedbackDto> UpdatePraiseAsync(Guid feedbackId, UpdatePraiseDto request, Guid userId)
    {
        var feedback = await feedbackRepository.GetByIdWithDetailsAsync(feedbackId);
        if (feedback == null)
            throw new EntityNotFoundException("Feedback not found");

        if (feedback.CoachUserId != userId)
            throw new ForbiddenException("Only the coach can modify this feedback");

        if (feedback.Praise == null)
            throw new EntityNotFoundException("Feedback has no praise");

        if (request.Message != null) feedback.Praise.Message = request.Message;
        if (request.BadgeType.HasValue) feedback.Praise.BadgeType = request.BadgeType;

        praiseRepository.Update(feedback.Praise);
        await praiseRepository.SaveChangesAsync();

        return await GetByIdAsync(feedbackId, userId) ?? throw new Exception("Failed to retrieve feedback");
    }

    public async Task<FeedbackDto> RemovePraiseAsync(Guid feedbackId, Guid userId)
    {
        var feedback = await feedbackRepository.GetByIdWithDetailsAsync(feedbackId);
        if (feedback == null)
            throw new EntityNotFoundException("Feedback not found");

        if (feedback.CoachUserId != userId)
            throw new ForbiddenException("Only the coach can modify this feedback");

        if (feedback.Praise != null)
        {
            feedback.Praise.IsDeleted = true;
            praiseRepository.Update(feedback.Praise);
            await praiseRepository.SaveChangesAsync();
        }

        return await GetByIdAsync(feedbackId, userId) ?? throw new Exception("Failed to retrieve feedback");
    }

    private async Task AddImprovementPointInternal(Guid feedbackId, CreateImprovementPointDto request, int order)
    {
        var point = mapper.Map<ImprovementPoint>(request);
        point.FeedbackId = feedbackId;
        point.Order = order;

        pointRepository.Add(point);
        await pointRepository.SaveChangesAsync();

        if (request.DrillIds != null)
        {
            foreach (var drillId in request.DrillIds)
            {
                var link = new ImprovementPointDrill
                {
                    ImprovementPointId = point.Id,
                    DrillId = drillId
                };
                drillLinkRepository.Add(link);
            }
            await drillLinkRepository.SaveChangesAsync();
        }

        if (request.MediaLinks != null)
        {
            foreach (var mediaDto in request.MediaLinks)
            {
                var media = mapper.Map<ImprovementPointMedia>(mediaDto);
                media.ImprovementPointId = point.Id;
                mediaRepository.Add(media);
            }
            await mediaRepository.SaveChangesAsync();
        }
    }

    private static string? StripHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return null;
        var doc = new AngleSharp.Html.Parser.HtmlParser().ParseDocument(html);
        var text = doc.Body?.TextContent ?? "";
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();
        return text.Length > 4000 ? text[..4000] : text;
    }
}
