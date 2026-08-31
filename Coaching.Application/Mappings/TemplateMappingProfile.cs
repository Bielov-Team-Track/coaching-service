using Coaching.Domain.Enums;
using AutoMapper;
using Coaching.Application.DTOs.Drills;
using Coaching.Application.DTOs.Templates;
using Coaching.Domain.Models.Templates;

namespace Coaching.Application.Mappings;

public class PlanMappingProfile : Profile
{
    public PlanMappingProfile()
    {
        // Plan mappings
        CreateMap<TrainingPlan, TrainingPlanDto>()
            .ForMember(d => d.DrillCount, opt => opt.MapFrom(s => s.Items.Count))
            .ForMember(d => d.SectionCount, opt => opt.MapFrom(s => s.Sections.Count))
            .ForMember(d => d.Skills, opt => opt.MapFrom(s =>
                s.Items
                    .Where(i => i.Drill != null)
                    .SelectMany(i => i.Drill!.Skills)
                    .Distinct()
                    .Select(sk => sk.ToString())
                    .ToList()))
            .ForMember(d => d.CommentCount, opt => opt.Ignore())
            .ForMember(d => d.Author, opt => opt.MapFrom(s => s.Creator != null
                ? new PlanAuthorDto
                {
                    Id = s.Creator.Id,
                    FirstName = s.Creator.Name,
                    LastName = s.Creator.Surname,
                    AvatarUrl = s.Creator.ImageUrl
                }
                : null))
            .ForMember(d => d.ClubName, opt => opt.Ignore()); // Club name requires cross-service lookup

        CreateMap<TrainingPlan, TrainingPlanDetailDto>()
            .ForMember(d => d.DrillCount, opt => opt.MapFrom(s => s.Items.Count))
            .ForMember(d => d.SectionCount, opt => opt.MapFrom(s => s.Sections.Count))
            .ForMember(d => d.Skills, opt => opt.MapFrom(s =>
                s.Items
                    .Where(i => i.Drill != null)
                    .SelectMany(i => i.Drill!.Skills)
                    .Distinct()
                    .Select(sk => sk.ToString())
                    .ToList()))
            .ForMember(d => d.CommentCount, opt => opt.Ignore())
            .ForMember(d => d.Author, opt => opt.MapFrom(s => s.Creator != null
                ? new PlanAuthorDto
                {
                    Id = s.Creator.Id,
                    FirstName = s.Creator.Name,
                    LastName = s.Creator.Surname,
                    AvatarUrl = s.Creator.ImageUrl
                }
                : null))
            .ForMember(d => d.ClubName, opt => opt.Ignore()); // Club name requires cross-service lookup

        CreateMap<CreatePlanDto, TrainingPlan>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.CreatedByUserId, opt => opt.Ignore())
            .ForMember(d => d.TotalDuration, opt => opt.Ignore())
            .ForMember(d => d.LikeCount, opt => opt.Ignore())
            .ForMember(d => d.UsageCount, opt => opt.Ignore())
            .ForMember(d => d.Sections, opt => opt.Ignore())
            .ForMember(d => d.Items, opt => opt.Ignore())
            .ForMember(d => d.Likes, opt => opt.Ignore())
            .ForMember(d => d.Bookmarks, opt => opt.Ignore())
            .ForMember(d => d.Comments, opt => opt.Ignore());

        // Section mappings
        CreateMap<PlanSection, PlanSectionDto>()
            .ForMember(d => d.Duration, opt => opt.MapFrom(s => s.Items.Sum(i => i.Duration)))
            .ForMember(d => d.Items, opt => opt.Ignore());

        CreateMap<CreatePlanSectionDto, PlanSection>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.TemplateId, opt => opt.Ignore())
            .ForMember(d => d.Plan, opt => opt.Ignore())
            .ForMember(d => d.Items, opt => opt.Ignore());

        // Item mappings - Drill is local, map it directly. IsCoached is resolved here so no
        // client has to re-derive it and get a different answer.
        // The values are not on the item — they hang off the plan, keyed to it — so the plan
        // read fills them in after the map rather than AutoMapper reaching for a member that
        // is deliberately not there.
        CreateMap<PlanItem, PlanItemDto>()
            .ForMember(d => d.Drill, opt => opt.MapFrom(s => s.Drill))
            .ForMember(d => d.IsCoached, opt => opt.MapFrom(s => s.Kind.IsCoached()))
            .ForMember(d => d.DialValues, opt => opt.Ignore())
            .ForMember(d => d.Stations, opt => opt.MapFrom(s => s.Stations.OrderBy(st => st.Order)));

        CreateMap<PlanStation, PlanStationDto>()
            .ForMember(d => d.Items, opt => opt.MapFrom(s => s.Items.OrderBy(i => i.Order)));

        CreateMap<PlanStationItem, PlanStationItemDto>()
            .ForMember(d => d.Drill, opt => opt.MapFrom(s => s.Drill))
            .ForMember(d => d.IsCoached, opt => opt.MapFrom(s => s.Kind.IsCoached()))
            .ForMember(d => d.DialValues, opt => opt.Ignore());

        CreateMap<CreatePlanItemDto, PlanItem>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.TemplateId, opt => opt.Ignore())
            .ForMember(d => d.Plan, opt => opt.Ignore())
            .ForMember(d => d.Section, opt => opt.Ignore())
            .ForMember(d => d.Drill, opt => opt.Ignore())
            .ForMember(d => d.Stations, opt => opt.Ignore())
            .ForMember(d => d.Order, opt => opt.Ignore());

        // Comment mappings
        CreateMap<PlanComment, PlanCommentDto>()
            .ForMember(d => d.User, opt => opt.MapFrom(s => s.User != null ? new UserProfileDto
            {
                Id = s.User.Id,
                FirstName = s.User.Name,
                LastName = s.User.Surname,
                AvatarUrl = s.User.ImageUrl
            } : null));

        // Bookmark mappings
        CreateMap<PlanBookmark, BookmarkedPlanDto>()
            .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Plan.Id))
            .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Plan.Name))
            .ForMember(d => d.TotalDuration, opt => opt.MapFrom(s => s.Plan.TotalDuration))
            .ForMember(d => d.DrillCount, opt => opt.MapFrom(s => s.Plan.Items.Count))
            .ForMember(d => d.LikeCount, opt => opt.MapFrom(s => s.Plan.LikeCount))
            .ForMember(d => d.BookmarkedAt, opt => opt.MapFrom(s => s.CreatedAt));
    }
}
