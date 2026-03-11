using AutoMapper;
using Coaching.Application.DTOs.Evaluation;
using Coaching.Domain.Models.Evaluation;

namespace Coaching.Application.Mappings;

public class EvaluationMappingProfile : Profile
{
    public EvaluationMappingProfile()
    {
        // Exercise mappings
        CreateMap<EvaluationExercise, EvaluationExerciseDto>();
        CreateMap<EvaluationMetric, EvaluationMetricDto>();
        CreateMap<MetricSkillWeight, MetricSkillWeightDto>();

        CreateMap<CreateEvaluationExerciseDto, EvaluationExercise>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.CreatedByUserId, opt => opt.Ignore())
            .ForMember(d => d.Metrics, opt => opt.Ignore())
            .ForMember(d => d.PlanItems, opt => opt.Ignore());

        CreateMap<CreateEvaluationMetricDto, EvaluationMetric>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.ExerciseId, opt => opt.Ignore())
            .ForMember(d => d.Exercise, opt => opt.Ignore())
            .ForMember(d => d.Order, opt => opt.Ignore())
            .ForMember(d => d.SkillWeights, opt => opt.Ignore())
            .ForMember(d => d.PlayerScores, opt => opt.Ignore());

        CreateMap<CreateMetricSkillWeightDto, MetricSkillWeight>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.MetricId, opt => opt.Ignore())
            .ForMember(d => d.Metric, opt => opt.Ignore());

        // Plan mappings
        CreateMap<EvaluationPlan, EvaluationPlanDto>();
        CreateMap<EvaluationPlanItem, EvaluationPlanItemDto>();

        CreateMap<CreateEvaluationPlanDto, EvaluationPlan>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.CreatedByUserId, opt => opt.Ignore())
            .ForMember(d => d.Items, opt => opt.Ignore())
            .ForMember(d => d.Sessions, opt => opt.Ignore());

        // Session mappings
        CreateMap<EvaluationSession, EvaluationSessionDto>();
        CreateMap<EvaluationParticipant, EvaluationParticipantDto>();

        // Player evaluation mappings
        CreateMap<PlayerEvaluation, PlayerEvaluationDto>();
        CreateMap<PlayerMetricScore, PlayerMetricScoreDto>()
            .ForMember(d => d.MetricName, opt => opt.MapFrom(s => s.Metric.Name));
        CreateMap<PlayerSkillScore, PlayerSkillScoreDto>();

        CreateMap<CreatePlayerEvaluationDto, PlayerEvaluation>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.EvaluationParticipantId, opt => opt.Ignore())
            .ForMember(d => d.EvaluatedByUserId, opt => opt.Ignore())
            .ForMember(d => d.Participant, opt => opt.Ignore())
            .ForMember(d => d.Outcome, opt => opt.Ignore())
            .ForMember(d => d.SharedWithPlayer, opt => opt.Ignore())
            .ForMember(d => d.MetricScores, opt => opt.Ignore())
            .ForMember(d => d.SkillScores, opt => opt.Ignore());

        // Threshold mappings
        CreateMap<EvaluationThreshold, EvaluationThresholdDto>();
    }
}
