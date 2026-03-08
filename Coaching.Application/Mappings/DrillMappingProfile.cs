using System.Text.Json;
using AutoMapper;
using Coaching.Application.DTOs.Drills;
using Coaching.Domain.Models.Drills;

namespace Coaching.Application.Mappings;

public class DrillMappingProfile : Profile
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public DrillMappingProfile()
    {
        // Drill mappings
        CreateMap<Drill, DrillDto>()
            .ForMember(dest => dest.Animations, opt => opt.MapFrom(src =>
                string.IsNullOrEmpty(src.Animations)
                    ? new List<DrillAnimationDto>()
                    : JsonSerializer.Deserialize<List<DrillAnimationDto>>(src.Animations, JsonOptions) ?? new List<DrillAnimationDto>()))
            .ForMember(dest => dest.Author, opt => opt.MapFrom(src => src.Creator != null
                ? new DrillAuthorDto
                {
                    Id = src.Creator.Id,
                    FirstName = src.Creator.Name,
                    LastName = src.Creator.Surname,
                    AvatarUrl = src.Creator.ImageUrl
                }
                : null))
            .ForMember(dest => dest.ClubName, opt => opt.Ignore()); // Club name requires cross-service lookup
        CreateMap<CreateDrillDto, Drill>();
        CreateMap<UpdateDrillDto, Drill>()
            .ForMember(d => d.Id, opt => opt.Ignore());

        // DrillVariation mappings
        CreateMap<DrillVariation, DrillVariationDto>()
            .ForMember(dest => dest.DrillId, opt => opt.MapFrom(src => src.TargetDrillId))
            .ForMember(dest => dest.DrillName, opt => opt.MapFrom(src => src.TargetDrill != null ? src.TargetDrill.Name : ""))
            .ForMember(dest => dest.DrillCategory, opt => opt.MapFrom(src => src.TargetDrill != null ? src.TargetDrill.Category : default))
            .ForMember(dest => dest.DrillIntensity, opt => opt.MapFrom(src => src.TargetDrill != null ? src.TargetDrill.Intensity : default));

        // DrillAttachment mappings
        CreateMap<DrillAttachment, DrillAttachmentDto>();
        CreateMap<CreateDrillAttachmentDto, DrillAttachment>();

        // DrillEquipment mappings
        CreateMap<DrillEquipment, DrillEquipmentDto>();

        // DrillComment mappings
        CreateMap<DrillComment, DrillCommentDto>()
            .ForMember(dest => dest.Author, opt => opt.MapFrom(src => src.User != null ? new DrillCommentAuthorDto
            {
                Id = src.User.Id,
                FirstName = src.User.Name,
                LastName = src.User.Surname,
                AvatarUrl = src.User.ImageUrl
            } : null));
    }
}
