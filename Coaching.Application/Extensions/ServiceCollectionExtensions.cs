using Coaching.Application.Interfaces.Services;
using Coaching.Application.Mappings;
using Coaching.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Coaching.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationMappings(this IServiceCollection services)
    {
        services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile<DrillMappingProfile>();
            cfg.AddProfile<PlanMappingProfile>();
            cfg.AddProfile<EvaluationMappingProfile>();
            cfg.AddProfile<FeedbackMappingProfile>();
        });
        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<Shared.Services.IActionRiskClassifier, DefaultActionRiskClassifier>();
        services.AddScoped<Shared.Services.IGuardianCacheService, CacheOnlyGuardianCacheService>();
        services.AddScoped<IDrillService, DrillService>();
        services.AddScoped<ITrainingPlanService, TrainingPlanService>();

        // Evaluation services
        services.AddScoped<IEvaluationExerciseService, EvaluationExerciseService>();
        services.AddScoped<IEvaluationPlanService, EvaluationPlanService>();
        services.AddScoped<IEvaluationSessionService, EvaluationSessionService>();
        services.AddScoped<IEvaluationSessionLifecycleService, EvaluationSessionLifecycleService>();
        services.AddScoped<IEvaluationGroupService, EvaluationGroupService>();
        services.AddScoped<IEvaluationScoringService, EvaluationScoringService>();
        services.AddScoped<IPlayerEvaluationService, PlayerEvaluationService>();
        services.AddScoped<IScoreCalculationService, ScoreCalculationService>();
        services.AddScoped<IThresholdService, ThresholdService>();
        services.AddScoped<IExportService, ExportService>();

        // Feedback services
        services.AddScoped<IFeedbackService, FeedbackService>();
        services.AddScoped<IFeedbackAuthorizationService, FeedbackAuthorizationService>();
        services.AddScoped<IBadgeService, BadgeService>();
        return services;
    }
}
