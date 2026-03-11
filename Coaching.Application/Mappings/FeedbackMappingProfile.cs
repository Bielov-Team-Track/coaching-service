using AutoMapper;
using Coaching.Application.DTOs.Feedback;
using Coaching.Domain.Models.Feedback;

namespace Coaching.Application.Mappings;

public class FeedbackMappingProfile : Profile
{
    public FeedbackMappingProfile()
    {
        CreateMap<Feedback, FeedbackDto>();
        CreateMap<ImprovementPoint, ImprovementPointDto>()
            .ForMember(d => d.AttachedDrills, opt => opt.MapFrom(s =>
                s.AttachedDrills.Select(ad => new AttachedDrillReferenceDto { DrillId = ad.DrillId })));
        CreateMap<ImprovementPointMedia, ImprovementPointMediaDto>();
        CreateMap<Praise, PraiseDto>();

        CreateMap<CreateFeedbackDto, Feedback>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.CoachUserId, opt => opt.Ignore())
            .ForMember(d => d.ImprovementPoints, opt => opt.Ignore())
            .ForMember(d => d.Praise, opt => opt.Ignore());

        CreateMap<CreateImprovementPointDto, ImprovementPoint>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.FeedbackId, opt => opt.Ignore())
            .ForMember(d => d.Feedback, opt => opt.Ignore())
            .ForMember(d => d.Order, opt => opt.Ignore())
            .ForMember(d => d.AttachedDrills, opt => opt.Ignore())
            .ForMember(d => d.MediaLinks, opt => opt.Ignore());

        CreateMap<CreateImprovementPointMediaDto, ImprovementPointMedia>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.ImprovementPointId, opt => opt.Ignore())
            .ForMember(d => d.ImprovementPoint, opt => opt.Ignore());

        CreateMap<CreatePraiseDto, Praise>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.FeedbackId, opt => opt.Ignore())
            .ForMember(d => d.Feedback, opt => opt.Ignore());
    }
}
