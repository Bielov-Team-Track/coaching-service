using AutoMapper;
using Coaching.Application.DTOs.Feedback;
using Coaching.Domain.Models.Feedback;

namespace Coaching.Application.Mappings;

public class FeedbackMappingProfile : Profile
{
    public FeedbackMappingProfile()
    {
        CreateMap<Feedback, FeedbackDto>()
            .ForMember(d => d.RecipientName, opt => opt.Ignore())
            .ForMember(d => d.RecipientImageUrl, opt => opt.Ignore())
            .ForMember(d => d.CoachName, opt => opt.Ignore())
            .ForMember(d => d.CoachImageUrl, opt => opt.Ignore());
        CreateMap<ImprovementPoint, ImprovementPointDto>()
            .ForMember(d => d.AttachedDrills, opt => opt.MapFrom(s =>
                s.AttachedDrills.Select(ad => new AttachedDrillReferenceDto { DrillId = ad.DrillId })));
        CreateMap<ImprovementPointMedia, ImprovementPointMediaDto>();
        CreateMap<Praise, PraiseDto>();

        CreateMap<CreateFeedbackDto, Feedback>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.CoachUserId, opt => opt.Ignore())
            .ForMember(d => d.ContentPlainText, opt => opt.Ignore())
            .ForMember(d => d.Evaluation, opt => opt.Ignore())
            .ForMember(d => d.ImprovementPoints, opt => opt.Ignore())
            .ForMember(d => d.Praise, opt => opt.Ignore())
            // Phase A: If Content is null but Comment is provided (old client), use Comment as Content
            .ForMember(d => d.Content, opt => opt.MapFrom(s => s.Content ?? s.Comment))
            // Phase A: Keep Comment column in sync for rollback safety
            .ForMember(d => d.Comment, opt => opt.Ignore()); // Set in service after sanitization

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
