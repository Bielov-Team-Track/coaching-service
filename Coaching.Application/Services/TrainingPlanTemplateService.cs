using AutoMapper;
using Coaching.Application.DTOs.Templates;
using Coaching.Application.Interfaces.Repositories;
using Coaching.Application.Interfaces.Services;
using Coaching.Domain.Enums;
using Coaching.Domain.Models.Templates;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Enums;
using Shared.Exceptions;
using Shared.Messaging.Contracts.Events.Coaching;

namespace Coaching.Application.Services;

public class TrainingPlanService : ITrainingPlanService
{
    private readonly ITrainingPlanRepository _planRepository;
    private readonly IPlanSectionRepository _sectionRepository;
    private readonly IPlanItemRepository _itemRepository;
    private readonly IPlanLikeRepository _likeRepository;
    private readonly IPlanBookmarkRepository _bookmarkRepository;
    private readonly IPlanCommentRepository _commentRepository;
    private readonly IDrillRepository _drillRepository;
    private readonly IClubsGrpcClient _clubsClient;
    private readonly IEventsGrpcClient _eventsGrpcClient;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IMapper _mapper;
    private readonly ILogger<TrainingPlanService> _logger;

    public TrainingPlanService(
        ITrainingPlanRepository planRepository,
        IPlanSectionRepository sectionRepository,
        IPlanItemRepository itemRepository,
        IPlanLikeRepository likeRepository,
        IPlanBookmarkRepository bookmarkRepository,
        IPlanCommentRepository commentRepository,
        IDrillRepository drillRepository,
        IClubsGrpcClient clubsClient,
        IEventsGrpcClient eventsGrpcClient,
        IPublishEndpoint publishEndpoint,
        IMapper mapper,
        ILogger<TrainingPlanService> logger)
    {
        _planRepository = planRepository;
        _sectionRepository = sectionRepository;
        _itemRepository = itemRepository;
        _likeRepository = likeRepository;
        _bookmarkRepository = bookmarkRepository;
        _commentRepository = commentRepository;
        _drillRepository = drillRepository;
        _clubsClient = clubsClient;
        _eventsGrpcClient = eventsGrpcClient;
        _publishEndpoint = publishEndpoint;
        _mapper = mapper;
        _logger = logger;
    }

    // =========================================================================
    // CRUD
    // =========================================================================

    public async Task<TrainingPlanDetailDto> CreateAsync(CreatePlanDto request, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new BadRequestException("Plan name is required", ErrorCodeEnum.ValidationError);

        var plan = new TrainingPlan
        {
            Name = request.Name,
            Description = request.Description,
            CreatedByUserId = userId,
            ClubId = request.ClubId,
            Visibility = request.Visibility,
            Level = request.Level,
            TotalDuration = 0,
            LikeCount = 0,
            UsageCount = 0
        };

        _planRepository.Add(plan);
        await _planRepository.SaveChangesAsync();

        // Add sections if provided
        if (request.Sections != null && request.Sections.Count > 0)
        {
            foreach (var sectionDto in request.Sections.OrderBy(s => s.Order))
            {
                var section = new PlanSection
                {
                    TemplateId = plan.Id,
                    Name = sectionDto.Name,
                    Order = sectionDto.Order
                };
                if (sectionDto.Id.HasValue)
                    section.Id = sectionDto.Id.Value;
                _sectionRepository.Add(section);
            }
            await _sectionRepository.SaveChangesAsync();
        }

        // Add items if provided
        if (request.Items != null && request.Items.Count > 0)
        {
            // Validate all drills exist locally
            var drillIds = request.Items.Select(i => i.DrillId).Distinct().ToList();
            foreach (var drillId in drillIds)
            {
                var drill = await _drillRepository.GetByIdAsync(drillId);
                if (drill == null)
                    throw new BadRequestException($"Drill not found: {drillId}", ErrorCodeEnum.EntityNotFound);
            }

            int order = 1;
            foreach (var itemDto in request.Items)
            {
                var item = new PlanItem
                {
                    TemplateId = plan.Id,
                    DrillId = itemDto.DrillId,
                    SectionId = itemDto.SectionId,
                    Duration = itemDto.Duration,
                    Notes = itemDto.Notes,
                    Order = itemDto.Order ?? order++
                };
                _itemRepository.Add(item);
            }
            await _itemRepository.SaveChangesAsync();

            // Recalculate total duration
            await RecalculateTotalDurationAsync(plan.Id);
        }

        // Re-fetch with details
        var created = await _planRepository.GetByIdWithDetailsAsync(plan.Id);
        var dto = _mapper.Map<TrainingPlanDetailDto>(created);
        await EnrichWithClubInfoAsync([dto]);
        return dto;
    }

    public async Task<TrainingPlanDetailDto?> GetByIdAsync(Guid id, Guid? userId = null)
    {
        var plan = await _planRepository.GetByIdWithDetailsAsync(id);
        if (plan == null) return null;

        // Check visibility permissions
        await ValidatePlanAccessAsync(plan, userId);

        var dto = _mapper.Map<TrainingPlanDetailDto>(plan);
        await EnrichWithClubInfoAsync([dto]);
        return dto;
    }

    public async Task<TrainingPlanDetailDto> UpdateAsync(Guid id, UpdatePlanDto request, Guid userId)
    {
        var plan = await _planRepository.GetByIdWithDetailsAsync(id);
        if (plan == null)
            throw new EntityNotFoundException("Plan not found");

        await ValidatePlanEditAsync(plan, userId);

        // Update fields
        if (request.Name != null)
            plan.Name = request.Name;

        if (request.Description != null)
            plan.Description = request.Description;

        if (request.ClubId.HasValue)
            plan.ClubId = request.ClubId.Value;

        if (request.Visibility.HasValue)
            plan.Visibility = request.Visibility.Value;

        if (request.Level.HasValue)
            plan.Level = request.Level.Value;

        plan.UpdatedAt = DateTime.UtcNow;

        _planRepository.Update(plan);
        await _planRepository.SaveChangesAsync();

        // Replace sections and items if provided (full replace strategy for wizard saves)
        if (request.Sections != null || request.Items != null)
        {
            // Delete existing items first (they reference sections via FK)
            var existingItems = await _itemRepository.Query()
                .Where(i => i.TemplateId == plan.Id)
                .ToListAsync();
            foreach (var item in existingItems)
                _itemRepository.Delete(item);
            await _itemRepository.SaveChangesAsync();

            // Delete existing sections
            var existingSections = await _sectionRepository.Query()
                .Where(s => s.TemplateId == plan.Id)
                .ToListAsync();
            foreach (var section in existingSections)
                _sectionRepository.Delete(section);
            await _sectionRepository.SaveChangesAsync();

            // Create new sections with client-provided IDs so items can reference them
            if (request.Sections != null)
            {
                foreach (var sectionDto in request.Sections.OrderBy(s => s.Order))
                {
                    var section = new PlanSection
                    {
                        TemplateId = plan.Id,
                        Name = sectionDto.Name,
                        Order = sectionDto.Order
                    };
                    if (sectionDto.Id.HasValue)
                        section.Id = sectionDto.Id.Value;
                    _sectionRepository.Add(section);
                }
                await _sectionRepository.SaveChangesAsync();
            }

            // Create new items
            if (request.Items != null && request.Items.Count > 0)
            {
                // Validate all drills exist locally
                var drillIds = request.Items.Select(i => i.DrillId).Distinct().ToList();
                foreach (var drillId in drillIds)
                {
                    var drill = await _drillRepository.GetByIdAsync(drillId);
                    if (drill == null)
                        throw new BadRequestException($"Drill not found: {drillId}", ErrorCodeEnum.EntityNotFound);
                }

                int order = 1;
                foreach (var itemDto in request.Items)
                {
                    var item = new PlanItem
                    {
                        TemplateId = plan.Id,
                        DrillId = itemDto.DrillId,
                        SectionId = itemDto.SectionId,
                        Duration = itemDto.Duration,
                        Notes = itemDto.Notes,
                        Order = itemDto.Order ?? order++
                    };
                    _itemRepository.Add(item);
                }
                await _itemRepository.SaveChangesAsync();
            }

            await RecalculateTotalDurationAsync(plan.Id);
        }

        // Re-fetch with details
        var updated = await _planRepository.GetByIdWithDetailsAsync(plan.Id);
        var dto = _mapper.Map<TrainingPlanDetailDto>(updated);
        await EnrichWithClubInfoAsync([dto]);

        // Publish event for Instance plans so events-service can update its summary
        if (updated != null && updated.PlanType == PlanType.Instance)
            await PublishPlanUpdatedAsync(updated, "Updated");

        return dto;
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var plan = await _planRepository.GetByIdWithDetailsAsync(id);
        if (plan == null)
            throw new EntityNotFoundException("Plan not found");

        await ValidatePlanEditAsync(plan, userId);

        // Capture plan data before deletion for event publishing
        var isInstance = plan.PlanType == PlanType.Instance;
        var eventId = plan.EventId;

        _planRepository.Delete(plan);
        await _planRepository.SaveChangesAsync();

        // Publish event for Instance plans so events-service clears its summary
        if (isInstance && eventId.HasValue)
        {
            await _publishEndpoint.Publish(new TrainingPlanUpdatedEvent
            {
                PlanId = id,
                TargetEventId = eventId,
                Action = "Deleted",
                PlanName = null,
                TotalDuration = 0,
                SectionCount = 0,
                DrillCount = 0
            });
        }
    }

    // =========================================================================
    // EVENT PLANS
    // =========================================================================

    public async Task<TrainingPlanDetailDto> CreateEventPlanAsync(Guid eventId, CreateEventPlanDto request, Guid userId)
    {
        // Verify user is event admin (organizer/co-organizer)
        var isAdmin = await _eventsGrpcClient.IsEventAdminAsync(eventId, userId);
        if (!isAdmin)
            throw new ForbiddenException("Only event admins can create training plans for events");

        // Check if event already has a plan
        var existingPlan = await _planRepository.Query()
            .FirstOrDefaultAsync(p => p.EventId == eventId && p.PlanType == PlanType.Instance && !p.IsDeleted);
        if (existingPlan != null)
            throw new BadRequestException("This event already has a training plan", ErrorCodeEnum.ValidationError);

        TrainingPlan? sourceTemplate = null;
        if (request.SourceTemplateId.HasValue)
        {
            sourceTemplate = await _planRepository.GetByIdWithDetailsAsync(request.SourceTemplateId.Value);
            if (sourceTemplate == null || sourceTemplate.PlanType != PlanType.Template)
                throw new EntityNotFoundException("Source template not found");
        }

        // Create the instance plan
        var plan = new TrainingPlan
        {
            Name = request.Name ?? sourceTemplate?.Name ?? "Training Plan",
            Description = request.Description ?? sourceTemplate?.Description,
            CreatedByUserId = userId,
            PlanType = PlanType.Instance,
            EventId = eventId,
            SourceTemplateId = request.SourceTemplateId,
            Visibility = TemplateVisibility.Private,
            TotalDuration = 0,
            LikeCount = 0,
            UsageCount = 0
        };

        _planRepository.Add(plan);
        await _planRepository.SaveChangesAsync();

        // Copy sections and items from source template, or use request body
        if (sourceTemplate != null)
        {
            await CopySectionsAndItemsAsync(sourceTemplate, plan.Id);

            // Atomic increment UsageCount on source template
            await _planRepository.Query()
                .Where(p => p.Id == sourceTemplate.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.UsageCount, p => p.UsageCount + 1));
        }
        else
        {
            // Use sections/items from request body
            if (request.Sections != null && request.Sections.Count > 0)
            {
                foreach (var sectionDto in request.Sections.OrderBy(s => s.Order))
                {
                    var section = new PlanSection
                    {
                        TemplateId = plan.Id,
                        Name = sectionDto.Name,
                        Order = sectionDto.Order
                    };
                    if (sectionDto.Id.HasValue)
                        section.Id = sectionDto.Id.Value;
                    _sectionRepository.Add(section);
                }
                await _sectionRepository.SaveChangesAsync();
            }

            if (request.Items != null && request.Items.Count > 0)
            {
                var drillIds = request.Items.Select(i => i.DrillId).Distinct().ToList();
                foreach (var drillId in drillIds)
                {
                    var drill = await _drillRepository.GetByIdAsync(drillId);
                    if (drill == null)
                        throw new BadRequestException($"Drill not found: {drillId}", ErrorCodeEnum.EntityNotFound);
                }

                int order = 1;
                foreach (var itemDto in request.Items)
                {
                    var item = new PlanItem
                    {
                        TemplateId = plan.Id,
                        DrillId = itemDto.DrillId,
                        SectionId = itemDto.SectionId,
                        Duration = itemDto.Duration,
                        Notes = itemDto.Notes,
                        Order = itemDto.Order ?? order++
                    };
                    _itemRepository.Add(item);
                }
                await _itemRepository.SaveChangesAsync();
            }
        }

        // Recalculate total duration
        await RecalculateTotalDurationAsync(plan.Id);

        // Re-fetch with details
        var created = await _planRepository.GetByIdWithDetailsAsync(plan.Id);
        var dto = _mapper.Map<TrainingPlanDetailDto>(created);
        await EnrichWithClubInfoAsync([dto]);

        // Publish event so events-service can update its summary
        if (created != null)
            await PublishPlanUpdatedAsync(created, "Created");

        return dto;
    }

    public async Task<TrainingPlanDetailDto?> GetByEventIdAsync(Guid eventId, Guid userId)
    {
        // Verify user is event participant (or admin)
        var (isParticipant, eventExists) = await _eventsGrpcClient.IsEventParticipantAsync(eventId, userId);

        if (!eventExists)
            throw new EntityNotFoundException("Event not found");

        if (!isParticipant)
            throw new ForbiddenException("Only event participants can view event training plans");

        var plan = await _planRepository.Query()
            .Include(p => p.Sections.OrderBy(s => s.Order))
            .Include(p => p.Items.OrderBy(i => i.Order))
                .ThenInclude(i => i.Drill)
            .Include(p => p.Creator)
            .FirstOrDefaultAsync(p => p.EventId == eventId && p.PlanType == PlanType.Instance && !p.IsDeleted);

        if (plan == null) return null;

        var dto = _mapper.Map<TrainingPlanDetailDto>(plan);
        await EnrichWithClubInfoAsync([dto]);
        return dto;
    }

    public async Task<TrainingPlanDetailDto> PromoteToTemplateAsync(Guid planId, PromotePlanDto request, Guid userId)
    {
        var plan = await _planRepository.GetByIdWithDetailsAsync(planId);
        if (plan == null)
            throw new EntityNotFoundException("Plan not found");

        if (plan.PlanType != PlanType.Instance)
            throw new BadRequestException("Only event plans can be promoted to templates", ErrorCodeEnum.ValidationError);

        // Verify user has access: is event admin or plan creator
        var hasAccess = plan.CreatedByUserId == userId;
        if (!hasAccess && plan.EventId.HasValue)
        {
            hasAccess = await _eventsGrpcClient.IsEventAdminAsync(plan.EventId.Value, userId);
        }
        if (!hasAccess)
            throw new ForbiddenException("Only the plan creator or event admin can promote this plan");

        // Create new template plan by copying
        var template = new TrainingPlan
        {
            Name = request.Name ?? plan.Name,
            Description = plan.Description,
            CreatedByUserId = userId,
            ClubId = request.ClubId,
            PlanType = PlanType.Template,
            Visibility = TemplateVisibility.Private,
            Level = plan.Level,
            TotalDuration = 0,
            LikeCount = 0,
            UsageCount = 0
        };

        _planRepository.Add(template);
        await _planRepository.SaveChangesAsync();

        // Copy sections and items
        await CopySectionsAndItemsAsync(plan, template.Id);

        // Recalculate total duration
        await RecalculateTotalDurationAsync(template.Id);

        // Re-fetch with details
        var created = await _planRepository.GetByIdWithDetailsAsync(template.Id);
        var dto = _mapper.Map<TrainingPlanDetailDto>(created);
        await EnrichWithClubInfoAsync([dto]);
        return dto;
    }

    // =========================================================================
    // LIST/BROWSE
    // =========================================================================

    public async Task<PlanListResponseDto> GetMyPlansAsync(Guid userId, PlanFilterRequest filter)
    {
        var query = _planRepository.Query()
            .Where(t => t.CreatedByUserId == userId && t.PlanType == PlanType.Template);

        var (items, totalCount) = await ApplyFiltersAndPaginationAsync(query, filter);

        var dtos = _mapper.Map<List<TrainingPlanDto>>(items);
        await EnrichWithClubInfoAsync(dtos);

        return new PlanListResponseDto
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize)
        };
    }

    public async Task<PlanListResponseDto> GetClubPlansAsync(Guid clubId, Guid userId, PlanFilterRequest filter)
    {
        // TODO: Verify user is club member when club service is available
        var query = _planRepository.Query()
            .Where(t => t.ClubId == clubId && t.PlanType == PlanType.Template && t.Visibility != TemplateVisibility.Private);

        var (items, totalCount) = await ApplyFiltersAndPaginationAsync(query, filter);

        var dtos = _mapper.Map<List<TrainingPlanDto>>(items);
        await EnrichWithClubInfoAsync(dtos);

        return new PlanListResponseDto
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize)
        };
    }

    public async Task<PlanListResponseDto> GetPublicPlansAsync(PlanFilterRequest filter)
    {
        var query = _planRepository.Query()
            .Where(t => t.PlanType == PlanType.Template && t.Visibility == TemplateVisibility.Public);

        var (items, totalCount) = await ApplyFiltersAndPaginationAsync(query, filter);

        var dtos = _mapper.Map<List<TrainingPlanDto>>(items);
        await EnrichWithClubInfoAsync(dtos);

        return new PlanListResponseDto
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize)
        };
    }

    public async Task<PlanListResponseDto> GetBookmarkedPlansAsync(Guid userId, PlanFilterRequest filter)
    {
        var skip = (filter.Page - 1) * filter.PageSize;
        var bookmarks = await _bookmarkRepository.GetByUserAsync(userId, skip, filter.PageSize);
        var totalCount = await _bookmarkRepository.GetCountByUserAsync(userId);

        var plans = bookmarks.Select(b => b.Plan).ToList();

        var dtos = _mapper.Map<List<TrainingPlanDto>>(plans);
        await EnrichWithClubInfoAsync(dtos);

        return new PlanListResponseDto
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize)
        };
    }

    // =========================================================================
    // SECTIONS
    // =========================================================================

    public async Task<PlanSectionDto> AddSectionAsync(Guid planId, CreatePlanSectionDto request, Guid userId)
    {
        var plan = await _planRepository.GetByIdAsync(planId);
        if (plan == null)
            throw new EntityNotFoundException("Plan not found");

        await ValidatePlanEditAsync(plan, userId);

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new BadRequestException("Section name is required", ErrorCodeEnum.ValidationError);

        var section = new PlanSection
        {
            TemplateId = planId,
            Name = request.Name,
            Order = request.Order
        };

        _sectionRepository.Add(section);
        await _sectionRepository.SaveChangesAsync();

        return _mapper.Map<PlanSectionDto>(section);
    }

    public async Task<PlanSectionDto> UpdateSectionAsync(Guid planId, Guid sectionId, UpdatePlanSectionDto request, Guid userId)
    {
        var plan = await _planRepository.GetByIdAsync(planId);
        if (plan == null)
            throw new EntityNotFoundException("Plan not found");

        await ValidatePlanEditAsync(plan, userId);

        var section = await _sectionRepository.GetByIdAsync(sectionId);
        if (section == null || section.TemplateId != planId)
            throw new EntityNotFoundException("Section not found");

        if (request.Name != null)
            section.Name = request.Name;

        if (request.Order.HasValue)
            section.Order = request.Order.Value;

        section.UpdatedAt = DateTime.UtcNow;

        _sectionRepository.Update(section);
        await _sectionRepository.SaveChangesAsync();

        return _mapper.Map<PlanSectionDto>(section);
    }

    public async Task DeleteSectionAsync(Guid planId, Guid sectionId, Guid userId)
    {
        var plan = await _planRepository.GetByIdAsync(planId);
        if (plan == null)
            throw new EntityNotFoundException("Plan not found");

        await ValidatePlanEditAsync(plan, userId);

        var section = await _sectionRepository.GetByIdAsync(sectionId);
        if (section == null || section.TemplateId != planId)
            throw new EntityNotFoundException("Section not found");

        // Set all items in this section to have null sectionId (ungrouped)
        var items = await _itemRepository.Query()
            .Where(i => i.SectionId == sectionId)
            .ToListAsync();

        foreach (var item in items)
        {
            item.SectionId = null;
            _itemRepository.Update(item);
        }

        _sectionRepository.Delete(section);
        await _sectionRepository.SaveChangesAsync();
    }

    // =========================================================================
    // ITEMS
    // =========================================================================

    public async Task<PlanItemDto> AddItemAsync(Guid planId, CreatePlanItemDto request, Guid userId)
    {
        var plan = await _planRepository.GetByIdAsync(planId);
        if (plan == null)
            throw new EntityNotFoundException("Plan not found");

        await ValidatePlanEditAsync(plan, userId);

        // Validate drill exists locally
        var drill = await _drillRepository.GetByIdAsync(request.DrillId);
        if (drill == null)
            throw new EntityNotFoundException("Drill not found");

        // Validate section if provided
        if (request.SectionId.HasValue)
        {
            var section = await _sectionRepository.GetByIdAsync(request.SectionId.Value);
            if (section == null || section.TemplateId != planId)
                throw new BadRequestException("Section not found", ErrorCodeEnum.EntityNotFound);
        }

        var maxOrder = await _itemRepository.GetMaxOrderAsync(planId);

        var item = new PlanItem
        {
            TemplateId = planId,
            DrillId = request.DrillId,
            SectionId = request.SectionId,
            Duration = request.Duration,
            Notes = request.Notes,
            Order = request.Order ?? (maxOrder + 1)
        };

        _itemRepository.Add(item);
        await _itemRepository.SaveChangesAsync();

        // Recalculate total duration
        await RecalculateTotalDurationAsync(planId);

        // Re-fetch item
        var created = await _itemRepository.GetByIdAsync(item.Id);
        return _mapper.Map<PlanItemDto>(created);
    }

    public async Task<PlanItemDto> UpdateItemAsync(Guid planId, Guid itemId, UpdatePlanItemDto request, Guid userId)
    {
        var plan = await _planRepository.GetByIdAsync(planId);
        if (plan == null)
            throw new EntityNotFoundException("Plan not found");

        await ValidatePlanEditAsync(plan, userId);

        var item = await _itemRepository.GetByIdAsync(itemId);
        if (item == null || item.TemplateId != planId)
            throw new EntityNotFoundException("Plan item not found");

        // Validate section if provided
        if (request.SectionId.HasValue)
        {
            var section = await _sectionRepository.GetByIdAsync(request.SectionId.Value);
            if (section == null || section.TemplateId != planId)
                throw new BadRequestException("Section not found", ErrorCodeEnum.EntityNotFound);
        }

        bool durationChanged = false;

        if (request.SectionId.HasValue)
            item.SectionId = request.SectionId.Value;

        if (request.Duration.HasValue && request.Duration.Value != item.Duration)
        {
            item.Duration = request.Duration.Value;
            durationChanged = true;
        }

        if (request.Notes != null)
            item.Notes = request.Notes;

        item.UpdatedAt = DateTime.UtcNow;

        _itemRepository.Update(item);
        await _itemRepository.SaveChangesAsync();

        // Recalculate total duration if changed
        if (durationChanged)
            await RecalculateTotalDurationAsync(planId);

        // Re-fetch item
        var updated = await _itemRepository.GetByIdAsync(item.Id);
        return _mapper.Map<PlanItemDto>(updated);
    }

    public async Task DeleteItemAsync(Guid planId, Guid itemId, Guid userId)
    {
        var plan = await _planRepository.GetByIdAsync(planId);
        if (plan == null)
            throw new EntityNotFoundException("Plan not found");

        await ValidatePlanEditAsync(plan, userId);

        var item = await _itemRepository.GetByIdAsync(itemId);
        if (item == null || item.TemplateId != planId)
            throw new EntityNotFoundException("Plan item not found");

        _itemRepository.Delete(item);
        await _itemRepository.SaveChangesAsync();

        // Recalculate total duration
        await RecalculateTotalDurationAsync(planId);
    }

    public async Task ReorderItemsAsync(Guid planId, ReorderPlanItemsDto request, Guid userId)
    {
        var plan = await _planRepository.GetByIdWithDetailsAsync(planId);
        if (plan == null)
            throw new EntityNotFoundException("Plan not found");

        await ValidatePlanEditAsync(plan, userId);

        var items = plan.Items.ToDictionary(i => i.Id);

        for (int i = 0; i < request.ItemIds.Count; i++)
        {
            var itemId = request.ItemIds[i];
            if (items.TryGetValue(itemId, out var item))
            {
                item.Order = i + 1;
                item.UpdatedAt = DateTime.UtcNow;
                _itemRepository.Update(item);
            }
        }

        await _itemRepository.SaveChangesAsync();
    }

    // =========================================================================
    // LIKES
    // =========================================================================

    public async Task<PlanLikeStatusDto> LikeAsync(Guid planId, Guid userId)
    {
        await ValidateIsTemplate(planId);

        var plan = await _planRepository.GetByIdAsync(planId);
        if (plan == null)
            throw new EntityNotFoundException("Plan not found");

        // Check if already liked
        var existingLike = await _likeRepository.GetByTemplateAndUserAsync(planId, userId);
        if (existingLike != null)
        {
            return new PlanLikeStatusDto
            {
                IsLiked = true,
                LikeCount = plan.LikeCount
            };
        }

        // Create like
        var like = new PlanLike
        {
            TemplateId = planId,
            UserId = userId
        };

        _likeRepository.Add(like);

        // Update denormalized count
        plan.LikeCount++;
        _planRepository.Update(plan);

        await _planRepository.SaveChangesAsync();

        return new PlanLikeStatusDto
        {
            IsLiked = true,
            LikeCount = plan.LikeCount
        };
    }

    public async Task<PlanLikeStatusDto> UnlikeAsync(Guid planId, Guid userId)
    {
        await ValidateIsTemplate(planId);

        var plan = await _planRepository.GetByIdAsync(planId);
        if (plan == null)
            throw new EntityNotFoundException("Plan not found");

        var existingLike = await _likeRepository.GetByTemplateAndUserAsync(planId, userId);
        if (existingLike == null)
        {
            return new PlanLikeStatusDto
            {
                IsLiked = false,
                LikeCount = plan.LikeCount
            };
        }

        _likeRepository.Delete(existingLike);

        // Update denormalized count
        plan.LikeCount = Math.Max(0, plan.LikeCount - 1);
        _planRepository.Update(plan);

        await _planRepository.SaveChangesAsync();

        return new PlanLikeStatusDto
        {
            IsLiked = false,
            LikeCount = plan.LikeCount
        };
    }

    public async Task<PlanLikeStatusDto> GetLikeStatusAsync(Guid planId, Guid userId)
    {
        var plan = await _planRepository.GetByIdAsync(planId);
        if (plan == null)
            throw new EntityNotFoundException("Plan not found");

        var existingLike = await _likeRepository.GetByTemplateAndUserAsync(planId, userId);

        return new PlanLikeStatusDto
        {
            IsLiked = existingLike != null,
            LikeCount = plan.LikeCount
        };
    }

    // =========================================================================
    // BOOKMARKS
    // =========================================================================

    public async Task<PlanBookmarkStatusDto> BookmarkAsync(Guid planId, Guid userId)
    {
        await ValidateIsTemplate(planId);

        var plan = await _planRepository.GetByIdAsync(planId);
        if (plan == null)
            throw new EntityNotFoundException("Plan not found");

        // Check if already bookmarked
        var existingBookmark = await _bookmarkRepository.GetByTemplateAndUserAsync(planId, userId);
        if (existingBookmark != null)
        {
            return new PlanBookmarkStatusDto { IsBookmarked = true };
        }

        // Create bookmark
        var bookmark = new PlanBookmark
        {
            TemplateId = planId,
            UserId = userId
        };

        _bookmarkRepository.Add(bookmark);
        await _bookmarkRepository.SaveChangesAsync();

        return new PlanBookmarkStatusDto { IsBookmarked = true };
    }

    public async Task<PlanBookmarkStatusDto> UnbookmarkAsync(Guid planId, Guid userId)
    {
        await ValidateIsTemplate(planId);

        var existingBookmark = await _bookmarkRepository.GetByTemplateAndUserAsync(planId, userId);
        if (existingBookmark == null)
        {
            return new PlanBookmarkStatusDto { IsBookmarked = false };
        }

        _bookmarkRepository.Delete(existingBookmark);
        await _bookmarkRepository.SaveChangesAsync();

        return new PlanBookmarkStatusDto { IsBookmarked = false };
    }

    // =========================================================================
    // COMMENTS
    // =========================================================================

    public async Task<PlanCommentDto> CreateCommentAsync(Guid planId, CreatePlanCommentDto request, Guid userId)
    {
        await ValidateIsTemplate(planId);

        var plan = await _planRepository.GetByIdAsync(planId);
        if (plan == null)
            throw new EntityNotFoundException("Plan not found");

        if (string.IsNullOrWhiteSpace(request.Content))
            throw new BadRequestException("Comment content is required", ErrorCodeEnum.ValidationError);

        // Validate parent comment if provided
        if (request.ParentCommentId.HasValue)
        {
            var parentComment = await _commentRepository.GetByIdAsync(request.ParentCommentId.Value);
            if (parentComment == null || parentComment.TemplateId != planId)
                throw new BadRequestException("Parent comment not found", ErrorCodeEnum.EntityNotFound);
        }

        var comment = new PlanComment
        {
            TemplateId = planId,
            UserId = userId,
            Content = request.Content,
            ParentCommentId = request.ParentCommentId
        };

        _commentRepository.Add(comment);
        await _commentRepository.SaveChangesAsync();

        // Re-fetch with details
        var createdComment = await _commentRepository.GetByIdWithDetailsAsync(comment.Id);
        return _mapper.Map<PlanCommentDto>(createdComment);
    }

    public async Task<PlanCommentsResponseDto> GetCommentsAsync(Guid planId, Guid? cursor, int limit)
    {
        var plan = await _planRepository.GetByIdAsync(planId);
        if (plan == null)
            throw new EntityNotFoundException("Plan not found");

        var comments = (await _commentRepository.GetByTemplateWithCursorAsync(planId, cursor, limit + 1)).ToList();
        var hasMore = comments.Count > limit;

        if (hasMore)
        {
            comments = comments.Take(limit).ToList();
        }

        var nextCursor = hasMore && comments.Count > 0 ? comments.Last().Id : (Guid?)null;

        return new PlanCommentsResponseDto
        {
            Items = _mapper.Map<List<PlanCommentDto>>(comments),
            NextCursor = nextCursor,
            HasMore = hasMore
        };
    }

    public async Task DeleteCommentAsync(Guid planId, Guid commentId, Guid userId)
    {
        var comment = await _commentRepository.GetByIdAsync(commentId);
        if (comment == null || comment.TemplateId != planId)
            throw new EntityNotFoundException("Comment not found");

        if (comment.UserId != userId)
            throw new ForbiddenException("Only the comment author can delete this comment");

        // Soft delete
        comment.IsDeleted = true;
        _commentRepository.Update(comment);
        await _commentRepository.SaveChangesAsync();
    }

    // =========================================================================
    // HELPER METHODS
    // =========================================================================

    /// <summary>
    /// Validates that the plan exists and is a Template (not an Instance).
    /// Call at the top of social methods to enforce that social features are template-only.
    /// </summary>
    private async Task ValidateIsTemplate(Guid planId)
    {
        var plan = await _planRepository.GetByIdAsync(planId);
        if (plan == null || plan.PlanType != PlanType.Template)
            throw new EntityNotFoundException("Plan not found");
    }

    /// <summary>
    /// Copies sections and items from a source plan to a target plan.
    /// Used by both CreateEventPlanAsync (copy from template) and PromoteToTemplateAsync (copy from instance).
    /// </summary>
    private async Task CopySectionsAndItemsAsync(TrainingPlan source, Guid targetPlanId)
    {
        // Map old section IDs to new section IDs so items reference the correct section
        var sectionIdMap = new Dictionary<Guid, Guid>();

        if (source.Sections.Count > 0)
        {
            foreach (var sourceSection in source.Sections.OrderBy(s => s.Order))
            {
                var newSection = new PlanSection
                {
                    TemplateId = targetPlanId,
                    Name = sourceSection.Name,
                    Order = sourceSection.Order
                };
                sectionIdMap[sourceSection.Id] = newSection.Id;
                _sectionRepository.Add(newSection);
            }
            await _sectionRepository.SaveChangesAsync();
        }

        if (source.Items.Count > 0)
        {
            foreach (var sourceItem in source.Items.OrderBy(i => i.Order))
            {
                var newItem = new PlanItem
                {
                    TemplateId = targetPlanId,
                    DrillId = sourceItem.DrillId,
                    SectionId = sourceItem.SectionId.HasValue && sectionIdMap.ContainsKey(sourceItem.SectionId.Value)
                        ? sectionIdMap[sourceItem.SectionId.Value]
                        : null,
                    Duration = sourceItem.Duration,
                    Notes = sourceItem.Notes,
                    Order = sourceItem.Order
                };
                _itemRepository.Add(newItem);
            }
            await _itemRepository.SaveChangesAsync();
        }
    }

    private async Task PublishPlanUpdatedAsync(TrainingPlan plan, string action)
    {
        if (plan.EventId == null) return;

        try
        {
            await _publishEndpoint.Publish(new TrainingPlanUpdatedEvent
            {
                PlanId = plan.Id,
                TargetEventId = plan.EventId,
                Action = action,
                PlanName = plan.Name,
                TotalDuration = plan.TotalDuration,
                SectionCount = plan.Sections?.Count ?? 0,
                DrillCount = plan.Items?.Count ?? 0
            });

            // Flush the EF outbox so the message is persisted and delivered
            await _planRepository.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish TrainingPlanUpdatedEvent for plan {PlanId}", plan.Id);
        }
    }

    private async Task RecalculateTotalDurationAsync(Guid planId)
    {
        var plan = await _planRepository.GetByIdAsync(planId);
        if (plan == null) return;

        var items = await _itemRepository.GetByTemplateAsync(planId);
        plan.TotalDuration = items.Sum(i => i.Duration);
        plan.UpdatedAt = DateTime.UtcNow;

        _planRepository.Update(plan);
        await _planRepository.SaveChangesAsync();
    }

    private async Task ValidatePlanAccessAsync(TrainingPlan plan, Guid? userId)
    {
        // Public plans can be viewed by anyone
        if (plan.Visibility == TemplateVisibility.Public)
            return;

        // Private plans require authentication
        if (!userId.HasValue)
            throw new ForbiddenException("This plan is private");

        // Owner can always access
        if (plan.CreatedByUserId == userId.Value)
            return;

        // Club plans: Check if user is club member when club service is available
        if (plan.ClubId.HasValue)
        {
            // TODO: Check club membership when club service is available
            // For now, allow club members (would need club service integration)
        }

        // Otherwise, deny access
        throw new ForbiddenException("You do not have permission to view this plan");
    }

    private async Task ValidatePlanEditAsync(TrainingPlan plan, Guid userId)
    {
        // Owner can always edit
        if (plan.CreatedByUserId == userId)
            return;

        // Club admins/coaches can edit club plans
        if (plan.ClubId.HasValue)
        {
            // TODO: Check if user is club admin/coach when club service is available
            // For now, only owner can edit
        }

        throw new ForbiddenException("Only the plan owner can modify this plan");
    }

    private async Task<(IEnumerable<TrainingPlan> items, int totalCount)> ApplyFiltersAndPaginationAsync(
        IQueryable<TrainingPlan> query,
        PlanFilterRequest filter)
    {
        // Apply search filter
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var searchLower = filter.SearchTerm.ToLower();
            query = query.Where(t =>
                t.Name.ToLower().Contains(searchLower) ||
                (t.Description != null && t.Description.ToLower().Contains(searchLower)));
        }

        // Apply duration filters
        if (filter.MinDuration.HasValue)
            query = query.Where(t => t.TotalDuration >= filter.MinDuration.Value);

        if (filter.MaxDuration.HasValue)
            query = query.Where(t => t.TotalDuration <= filter.MaxDuration.Value);

        // Apply level filter
        if (filter.Level.HasValue)
            query = query.Where(t => t.Level == filter.Level.Value);

        // TODO: Skills filter would require joining with Items and Drills
        // Skipping for now as it's more complex

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply sorting
        query = filter.SortBy?.ToLower() switch
        {
            "name" => query.OrderBy(t => t.Name),
            "duration" => query.OrderBy(t => t.TotalDuration),
            "likes" => query.OrderByDescending(t => t.LikeCount),
            "usage" => query.OrderByDescending(t => t.UsageCount),
            "oldest" => query.OrderBy(t => t.CreatedAt),
            _ => query.OrderByDescending(t => t.CreatedAt) // newest by default
        };

        // Apply pagination
        var skip = (filter.Page - 1) * filter.PageSize;
        var items = await query
            .Skip(skip)
            .Take(filter.PageSize)
            .Include(t => t.Items)
                .ThenInclude(i => i.Drill)
            .Include(t => t.Sections)
            .Include(t => t.Creator)
            .ToListAsync();

        return (items, totalCount);
    }

    /// <summary>
    /// Enriches plan DTOs with club info from clubs-service (batch operation for performance)
    /// </summary>
    private async Task EnrichWithClubInfoAsync(IEnumerable<TrainingPlanDto> plans)
    {
        var planList = plans.ToList();
        if (planList.Count == 0) return;

        // Collect all club IDs that need to be fetched
        var clubIds = planList
            .Where(t => t.ClubId.HasValue)
            .Select(t => t.ClubId!.Value)
            .Distinct()
            .ToList();

        if (clubIds.Count == 0) return;

        try
        {
            // Batch fetch club info
            var clubInfos = await _clubsClient.GetClubInfoAsync(clubIds);

            // Enrich DTOs
            foreach (var plan in planList)
            {
                if (plan.ClubId.HasValue && clubInfos.TryGetValue(plan.ClubId.Value, out var clubInfo))
                {
                    plan.ClubName = clubInfo.Name;
                    plan.ClubLogoUrl = clubInfo.LogoUrl;
                }
            }
        }
        catch (Exception ex)
        {
            // Log but don't fail - club info is nice-to-have
            _logger.LogWarning(ex, "Failed to enrich plans with club info");
        }
    }
}
