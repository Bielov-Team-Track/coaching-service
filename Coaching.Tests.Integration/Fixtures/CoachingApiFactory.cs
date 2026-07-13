using System.Text;
using Coaching.Application.Interfaces.Services;
using Coaching.Infrastructure.Data.Context;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using Shared.Testing.Fixtures;

namespace Coaching.Tests.Integration.Fixtures;

public class CoachingApiFactory : WebApplicationFactory<Program>
{
    private readonly PostgresFixture _postgresFixture = new();
    public DatabaseResetter DatabaseResetter { get; private set; } = null!;

    public const string JwtSecret = "314b7dfbe6cf5a56208d194297589c4bb12e07410c88c576044bddc4da82f884";
    public const string JwtIssuer = "AuthService";
    public const string JwtAudience = "AuthService-Users";

    public IEventsGrpcClient EventsGrpcClient { get; private set; } = null!;
    public IClubsGrpcClient ClubsGrpcClient { get; private set; } = null!;
    public IRunBroadcaster RunBroadcaster { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _postgresFixture.InitializeAsync();
        DatabaseResetter = new DatabaseResetter(_postgresFixture.ConnectionString);
        EventsGrpcClient = Substitute.For<IEventsGrpcClient>();
        ClubsGrpcClient = Substitute.For<IClubsGrpcClient>();
        RunBroadcaster = Substitute.For<IRunBroadcaster>();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var dbDescriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<CoachingDbContext>));
            if (dbDescriptor != null)
                services.Remove(dbDescriptor);

            services.AddDbContext<CoachingDbContext>(options =>
                options.UseNpgsql(_postgresFixture.ConnectionString));

            ReplaceWithSingleton(services, EventsGrpcClient);
            ReplaceWithSingleton(services, ClubsGrpcClient);
            ReplaceWithSingleton(services, RunBroadcaster);

            // Remove MassTransit hosted services to prevent RabbitMQ connection attempts.
            var massTransitHosted = services.Where(d =>
                d.ServiceType == typeof(IHostedService) &&
                (d.ImplementationType?.FullName?.Contains("MassTransit") == true ||
                 d.ImplementationFactory?.Method.DeclaringType?.FullName?.Contains("MassTransit") == true))
                .ToList();
            foreach (var d in massTransitHosted)
                services.Remove(d);

            var publishEndpoints = services.Where(d => d.ServiceType == typeof(IPublishEndpoint)).ToList();
            foreach (var d in publishEndpoints)
                services.Remove(d);
            services.AddSingleton(Substitute.For<IPublishEndpoint>());

            var cacheDescriptors = services.Where(d => d.ServiceType == typeof(IDistributedCache)).ToList();
            foreach (var d in cacheDescriptors)
                services.Remove(d);
            services.AddDistributedMemoryCache();

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(JwtSecret)),
                        ValidateIssuer = true,
                        ValidIssuer = JwtIssuer,
                        ValidateAudience = true,
                        ValidAudience = JwtAudience,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    };
                });

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CoachingDbContext>();
            db.Database.Migrate();
        });

        builder.UseEnvironment("Testing");
    }

    private static void ReplaceWithSingleton<T>(IServiceCollection services, T instance) where T : class
    {
        var descriptors = services.Where(d => d.ServiceType == typeof(T)).ToList();
        foreach (var d in descriptors)
            services.Remove(d);
        services.AddSingleton(instance);
    }

    public new async Task DisposeAsync()
    {
        await _postgresFixture.DisposeAsync();
        await base.DisposeAsync();
    }
}
