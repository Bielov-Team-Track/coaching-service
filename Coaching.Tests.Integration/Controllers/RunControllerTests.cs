using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Coaching.Application.DTOs.Templates;
using Coaching.Application.Interfaces.Services;
using Coaching.Domain.Enums;
using Coaching.Domain.Models.Drills;
using Coaching.Domain.Models.Templates;
using Coaching.Infrastructure.Data.Context;
using Coaching.Tests.Integration.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using Shared.Models;

namespace Coaching.Tests.Integration.Controllers;

[TestFixture]
[Category("Integration")]
public class RunControllerTests
{
    private CoachingApiFactory _factory = null!;
    private HttpClient _client = null!;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    private static readonly Guid CreatorId = Guid.NewGuid();
    private static readonly Guid OtherUserId = Guid.NewGuid();

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _factory = new CoachingApiFactory();
        await _factory.InitializeAsync();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _factory.DisposeAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        await _factory.DatabaseResetter.ResetAsync();
        _factory.EventsGrpcClient.ClearReceivedCalls();
        _factory.RunBroadcaster.ClearReceivedCalls();
    }

    [Test]
    public async Task GetRun_NoRunStarted_Returns200WithNull()
    {
        // Arrange
        var (eventId, _, _) = await SeedPlanWithTwoItemsAsync();
        StubParticipant(eventId);
        SetAuth(CreatorId);

        // Act
        var response = await _client.GetAsync($"/v1/events/{eventId}/plans/run");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Trim().Should().BeOneOf("null", string.Empty);
    }

    [Test]
    public async Task GetRun_Unauthenticated_Returns401()
    {
        // Arrange
        var (eventId, _, _) = await SeedPlanWithTwoItemsAsync();

        // Act
        var response = await _client.GetAsync($"/v1/events/{eventId}/plans/run");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task StartRun_AsPlanCreator_Returns200RunningWithFirstItemCurrent()
    {
        // Arrange
        var (eventId, item1Id, _) = await SeedPlanWithTwoItemsAsync();
        SetAuth(CreatorId);

        // Act
        var response = await _client.PostAsync($"/v1/events/{eventId}/plans/run/start", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var run = await response.Content.ReadFromJsonAsync<RunDto>(JsonOptions);
        run!.Status.Should().Be(RunStatus.Running);
        run.CurrentItemId.Should().Be(item1Id);
        run.CanControl.Should().BeTrue();
        run.Items.Should().HaveCount(2);
        run.ServerTime.Should().NotBe(default);
    }

    [Test]
    public async Task StartRun_AsNonCreator_Returns403()
    {
        // Arrange
        var (eventId, _, _) = await SeedPlanWithTwoItemsAsync();
        SetAuth(OtherUserId);

        // Act
        var response = await _client.PostAsync($"/v1/events/{eventId}/plans/run/start", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task StartRun_NoPlanForEvent_Returns404()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        SetAuth(CreatorId);

        // Act
        var response = await _client.PostAsync($"/v1/events/{eventId}/plans/run/start", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task StartRun_BroadcastsRunUpdated()
    {
        // Arrange
        var (eventId, _, _) = await SeedPlanWithTwoItemsAsync();
        SetAuth(CreatorId);

        // Act
        await _client.PostAsync($"/v1/events/{eventId}/plans/run/start", null);

        // Assert
        await _factory.RunBroadcaster.Received(1)
            .BroadcastRunUpdatedAsync(eventId, Arg.Is<RunDto>(d => d.Status == RunStatus.Running));
    }

    [Test]
    public async Task PauseResume_RoundTrips()
    {
        // Arrange
        var (eventId, _, _) = await SeedPlanWithTwoItemsAsync();
        SetAuth(CreatorId);
        await _client.PostAsync($"/v1/events/{eventId}/plans/run/start", null);

        // Act
        var pauseResp = await _client.PostAsync($"/v1/events/{eventId}/plans/run/pause", null);
        var resumeResp = await _client.PostAsync($"/v1/events/{eventId}/plans/run/resume", null);

        // Assert
        var paused = await pauseResp.Content.ReadFromJsonAsync<RunDto>(JsonOptions);
        paused!.Status.Should().Be(RunStatus.Paused);
        paused.CurrentItemStartedAt.Should().BeNull();

        var resumed = await resumeResp.Content.ReadFromJsonAsync<RunDto>(JsonOptions);
        resumed!.Status.Should().Be(RunStatus.Running);
        resumed.CurrentItemStartedAt.Should().NotBeNull();
    }

    [Test]
    public async Task Advance_ThenAdvanceLast_CompletesRun()
    {
        // Arrange
        var (eventId, item1Id, item2Id) = await SeedPlanWithTwoItemsAsync();
        SetAuth(CreatorId);
        await _client.PostAsync($"/v1/events/{eventId}/plans/run/start", null);

        // Act
        var first = await _client.PostAsJsonAsync($"/v1/events/{eventId}/plans/run/advance", new AdvanceRunDto(item1Id), JsonOptions);
        var second = await _client.PostAsJsonAsync($"/v1/events/{eventId}/plans/run/advance", new AdvanceRunDto(item2Id), JsonOptions);

        // Assert
        var afterFirst = await first.Content.ReadFromJsonAsync<RunDto>(JsonOptions);
        afterFirst!.CurrentItemId.Should().Be(item2Id);
        afterFirst.Status.Should().Be(RunStatus.Running);

        var afterSecond = await second.Content.ReadFromJsonAsync<RunDto>(JsonOptions);
        afterSecond!.Status.Should().Be(RunStatus.Completed);
        afterSecond.CurrentItemId.Should().BeNull();
        afterSecond.CompletedAt.Should().NotBeNull();
    }

    [Test]
    public async Task Advance_FromItemMismatch_LeavesRunUnchanged()
    {
        // Arrange
        var (eventId, item1Id, item2Id) = await SeedPlanWithTwoItemsAsync();
        SetAuth(CreatorId);
        await _client.PostAsync($"/v1/events/{eventId}/plans/run/start", null);

        // Act — claim we're leaving item2 while the run is on item1.
        var response = await _client.PostAsJsonAsync($"/v1/events/{eventId}/plans/run/advance", new AdvanceRunDto(item2Id), JsonOptions);

        // Assert
        var run = await response.Content.ReadFromJsonAsync<RunDto>(JsonOptions);
        run!.CurrentItemId.Should().Be(item1Id);
        run.Status.Should().Be(RunStatus.Running);
    }

    [Test]
    public async Task Complete_Returns200Completed()
    {
        // Arrange
        var (eventId, _, _) = await SeedPlanWithTwoItemsAsync();
        SetAuth(CreatorId);
        await _client.PostAsync($"/v1/events/{eventId}/plans/run/start", null);

        // Act
        var response = await _client.PostAsync($"/v1/events/{eventId}/plans/run/complete", null);

        // Assert
        var run = await response.Content.ReadFromJsonAsync<RunDto>(JsonOptions);
        run!.Status.Should().Be(RunStatus.Completed);
        run.CurrentItemId.Should().BeNull();
    }

    [Test]
    public async Task Pause_NoRun_Returns404()
    {
        // Arrange
        var (eventId, _, _) = await SeedPlanWithTwoItemsAsync();
        SetAuth(CreatorId);

        // Act
        var response = await _client.PostAsync($"/v1/events/{eventId}/plans/run/pause", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ---------- Helpers ----------

    private void StubParticipant(Guid eventId) =>
        _factory.EventsGrpcClient.IsEventParticipantAsync(eventId, Arg.Any<Guid>()).Returns((true, true));

    private async Task<(Guid eventId, Guid item1Id, Guid item2Id)> SeedPlanWithTwoItemsAsync()
    {
        var eventId = Guid.NewGuid();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CoachingDbContext>();

        db.Set<UserProfile>().Add(new UserProfile
        {
            Id = CreatorId,
            Name = "Coach",
            Surname = "Creator",
            Email = "coach.creator@test.com"
        });

        var drill1 = new Drill { Id = Guid.NewGuid(), Name = "Drill 1", CreatedByUserId = CreatorId };
        var drill2 = new Drill { Id = Guid.NewGuid(), Name = "Drill 2", CreatedByUserId = CreatorId };
        db.Drills.AddRange(drill1, drill2);

        var planId = Guid.NewGuid();
        var item1Id = Guid.NewGuid();
        var item2Id = Guid.NewGuid();
        db.TrainingPlans.Add(new TrainingPlan
        {
            Id = planId,
            Name = "Instance Plan",
            CreatedByUserId = CreatorId,
            PlanType = PlanType.Instance,
            EventId = eventId,
            Visibility = TemplateVisibility.Private,
            Items = new List<PlanItem>
            {
                new() { Id = item1Id, TemplateId = planId, DrillId = drill1.Id, Order = 1, Duration = 5 },
                new() { Id = item2Id, TemplateId = planId, DrillId = drill2.Id, Order = 2, Duration = 10 }
            }
        });

        await db.SaveChangesAsync();
        return (eventId, item1Id, item2Id);
    }

    private void SetAuth(Guid userId)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwt(userId));
    }

    private static string GenerateJwt(Guid userId, string email = "test@example.com")
    {
        var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(CoachingApiFactory.JwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.NameId, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var token = new JwtSecurityToken(
            issuer: CoachingApiFactory.JwtIssuer,
            audience: CoachingApiFactory.JwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
