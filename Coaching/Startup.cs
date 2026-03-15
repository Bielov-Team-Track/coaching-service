using Asp.Versioning;
using Coaching.Application.Extensions;
using Coaching.Infrastructure.Data.Context;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Coaching.Application.Interfaces.Repositories;
using Coaching.Application.Interfaces.Services;
using Coaching.Infrastructure.Repositories;
using Coaching.Infrastructure.Services;
using Shared.Contracts.Grpc;
using Shared.DataAccess.Extensions;
using Shared.DataAccess.Repositories;
using Shared.DataAccess.Repositories.Interfaces;
using Coaching.Application.Consumers;
using Shared.Messaging.Consumers;
using Shared.Messaging.Extensions;
using Shared.Options;
using Shared.Services.Extensions;
using System.Text;
using Shared.Middleware;
using Shared.Extensions;
using Shared.Microservices.Extensions;
using OpenTelemetry.Trace;

namespace Coaching
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public IWebHostEnvironment Environment { get; }

        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers()
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver();
                    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                    options.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
                });

            services.ConfigureProblemDetailsValidation();

            // API Versioning
            services.AddApiVersioning(opt =>
            {
                opt.DefaultApiVersion = new ApiVersion(1, 0);
                opt.AssumeDefaultVersionWhenUnspecified = true;
                opt.ApiVersionReader = ApiVersionReader.Combine(
                    new UrlSegmentApiVersionReader(),
                    new QueryStringApiVersionReader("version"),
                    new HeaderApiVersionReader("X-Version")
                );
            });
            services.AddApiVersioning();
            services.AddGrpc();
            services.AddGrpcHealthChecks();

            // Database
            var connectionString = Configuration.GetConnectionString("DefaultConnection");
            services.AddDbContext<CoachingDbContext>(options =>
                options.UseNpgsql(connectionString));

            // Bind DbContext for BaseRepository
            services.AddScoped<DbContext>(provider => provider.GetRequiredService<CoachingDbContext>());

            // Repositories
            services.AddScoped(typeof(IRepository<>), typeof(BaseRepository<>));
            services.AddScoped<IDrillRepository, DrillRepository>();
            services.AddScoped<IDrillLikeRepository, DrillLikeRepository>();
            services.AddScoped<IDrillBookmarkRepository, DrillBookmarkRepository>();
            services.AddScoped<IDrillCommentRepository, DrillCommentRepository>();
            services.AddScoped<IDrillAttachmentRepository, DrillAttachmentRepository>();

            // Plan repositories
            services.AddScoped<ITrainingPlanRepository, TrainingPlanRepository>();
            services.AddScoped<IPlanSectionRepository, PlanSectionRepository>();
            services.AddScoped<IPlanItemRepository, PlanItemRepository>();
            services.AddScoped<IPlanLikeRepository, PlanLikeRepository>();
            services.AddScoped<IPlanBookmarkRepository, PlanBookmarkRepository>();
            services.AddScoped<IPlanCommentRepository, PlanCommentRepository>();

            // Feedback repositories
            services.AddScoped<IFeedbackRepository, FeedbackRepository>();

            // Evaluation repositories
            services.AddScoped<IEvaluationExerciseRepository, EvaluationExerciseRepository>();
            services.AddScoped<IEvaluationPlanRepository, EvaluationPlanRepository>();
            services.AddScoped<IEvaluationSessionRepository, EvaluationSessionRepository>();
            services.AddScoped<IEvaluationParticipantRepository, EvaluationParticipantRepository>();
            services.AddScoped<IPlayerEvaluationRepository, PlayerEvaluationRepository>();

            // gRPC Clients
            var clubsGrpcAddress = Configuration["GrpcClients:ClubsService"] ?? "http://clubs-service:5021";
            services.AddGrpcClient<ClubsInternalService.ClubsInternalServiceClient>(o =>
            {
                o.Address = new Uri(clubsGrpcAddress);
            });
            services.AddScoped<IClubsGrpcClient, ClubsGrpcClient>();

            var eventsGrpcAddress = Configuration["GrpcClients:EventsService"] ?? "http://events-service:5011";
            services.AddGrpcClient<EventsInternalService.EventsInternalServiceClient>(o =>
            {
                o.Address = new Uri(eventsGrpcAddress);
            });
            services.AddScoped<IEventsGrpcClient, EventsGrpcClient>();

            // AutoMapper & Application services
            services.AddApplicationMappings();
            services.AddApplicationServices();

            services.AddSharedDataAccess();

            // S3 Settings
            services.Configure<S3Settings>(Configuration.GetSection("S3"));
            services.AddDefaultAWSOptions(Configuration.GetAWSOptions());
            services.AddSharedServices();

            // Caching
            services.AddMemoryCache();
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = Configuration.GetValue<string>("Redis:ConnectionString");
            });

            // JWT & Auth
            services.Configure<JwtSettings>(Configuration.GetSection("Jwt"));
            var jwtSettings = Configuration.GetSection("Jwt").Get<JwtSettings>();

            services.AddMessaging<CoachingDbContext>(options =>
            {
                options.Host = Configuration["RabbitMQ:Host"] ?? "localhost";
                options.Port = ushort.Parse(Configuration["RabbitMQ:Port"] ?? "5672");
                options.VirtualHost = Configuration["RabbitMQ:VirtualHost"] ?? "/";
                options.Username = Configuration["RabbitMQ:Username"] ?? "guest";
                options.Password = Configuration["RabbitMQ:Password"] ?? "guest";
                options.ServicePrefix = "coaching";
            },
            bus =>
            {
                bus.AddConsumer<UserProfileUpdatedConsumer>();
                bus.AddConsumer<EventDeletedConsumer>();
            });

            if (jwtSettings != null)
            {
                services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuerSigningKey = true,
                            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSettings.Secret)),
                            ValidateIssuer = true,
                            ValidIssuer = jwtSettings.Issuer,
                            ValidateAudience = true,
                            ValidAudience = jwtSettings.Audience,
                            ValidateLifetime = true,
                            ClockSkew = TimeSpan.Zero
                        };
                    });
            }

            services.AddAuthorization();

            // Swagger
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            // CORS
            var allowedOrigins = Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? new[] { "http://localhost:3000" };

            services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            // Health checks
            services.AddHealthChecks();
            services.AddPrometheusMetrics(Configuration);
            services.AddTracing(Configuration, "coaching-service", tracing =>
            {
                tracing.AddEntityFrameworkCoreInstrumentation();
                tracing.AddGrpcClientInstrumentation();
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseCors("AllowFrontend");

            // Prometheus HTTP request metrics
            app.UsePrometheusMetrics();

            if (env.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseRouting();

            app.UseAuthentication();
            app.UseMiddleware<JwtBlacklistMiddleware>();
            app.UseMiddleware<GuardianContextMiddleware>();
            app.UseAuthorization();

            app.UseMiddleware<ErrorHandlerMiddleware>();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapGrpcService<Grpc.CoachingInternalServiceImpl>();
                endpoints.MapHealthChecks("/health");
                endpoints.MapGrpcHealthChecksService();
            });
        }
    }
}
