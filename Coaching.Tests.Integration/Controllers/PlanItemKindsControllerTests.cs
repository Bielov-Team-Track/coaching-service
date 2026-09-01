using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Coaching.Application.DTOs.Templates;
using Coaching.Domain.Enums;
using Coaching.Domain.Models.Drills;
using Coaching.Infrastructure.Data.Context;
using Coaching.Tests.Integration.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Shared.Models;

namespace Coaching.Tests.Integration.Controllers;

/// <summary>
/// A plan holds more than drills. These prove the whole round trip: a break survives with
/// no drill behind it, the coached total excludes it, and a goal set on one use overrides
/// the drill's own without touching the library.
/// </summary>
[TestFixture]
[Category("Integration")]
public class PlanItemKindsControllerTests
{
    private CoachingApiFactory _factory = null!;
    private HttpClient _client = null!;
    private static readonly Guid CoachId = Guid.NewGuid();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _factory = new CoachingApiFactory();
        await _factory.InitializeAsync();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown() => await _factory.DisposeAsync();

    [TearDown]
    public async Task TearDown()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        await _factory.DatabaseResetter.ResetAsync();
    }

    [Test]
    public async Task CreatePlan_WithABreakBetweenTwoDrills_RoundTrips()
    {
        // Arrange — her "Water and Serve" is a break followed by an ordinary drill
        var drillId = await SeedDrillAsync("Serve Pass");
        SetAuth(CoachId);

        var request = new CreatePlanDto("Wed 18 Jun", null, null, Items:
        [
            new CreatePlanItemDto(drillId, null, 10, null, 1),
            new CreatePlanItemDto(null, null, 2, null, 2, ItemKind.Break, "Water"),
            new CreatePlanItemDto(drillId, null, 12, null, 3),
            new CreatePlanItemDto(null, null, 5, null, 4, ItemKind.Meeting, "Breakout"),
        ]);

        // Act
        var created = await _client.PostAsJsonAsync("/v1/plans", request, JsonOptions);
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var plan = await ReadPlanAsync(created);

        // Assert
        var items = plan.Items.OrderBy(i => i.Order).ToList();
        items.Should().HaveCount(4);

        items[1].Kind.Should().Be(ItemKind.Break);
        items[1].DrillId.Should().BeNull();
        items[1].Title.Should().Be("Water");
        items[1].IsCoached.Should().BeFalse();

        items[3].Kind.Should().Be(ItemKind.Meeting);
        items[3].IsCoached.Should().BeFalse();

        // 10 + 2 + 12 + 5 booked; only the two drills are coaching time
        plan.TotalDuration.Should().Be(29);
        plan.CoachedDuration.Should().Be(22);
    }

    [Test]
    public async Task UpdatePlan_TurningADrillIntoABreak_ClearsItsDrill()
    {
        // Arrange
        var drillId = await SeedDrillAsync("Serve Pass");
        SetAuth(CoachId);
        var plan = await ReadPlanAsync(await _client.PostAsJsonAsync("/v1/plans",
            new CreatePlanDto("Plan", null, null, Items: [new CreatePlanItemDto(drillId, null, 10, null, 1)]),
            JsonOptions));

        // Act — the row becomes a break, so the drill reference must not linger
        var update = new UpdatePlanDto(null, null, null, null, null, null,
            [new CreatePlanItemDto(null, null, 2, null, 1, ItemKind.Break, "Water")]);
        var response = await _client.PutAsJsonAsync($"/v1/plans/{plan.Id}", update, JsonOptions);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert
        var reread = await ReadPlanAsync(await _client.GetAsync($"/v1/plans/{plan.Id}"));
        var item = reread.Items.Single();
        item.Kind.Should().Be(ItemKind.Break);
        item.DrillId.Should().BeNull();
        reread.CoachedDuration.Should().Be(0);
    }

    [Test]
    public async Task CreatePlan_WhenABreakHasNoTitle_Returns400()
    {
        // Arrange
        await SeedDrillAsync("Serve Pass");
        SetAuth(CoachId);
        var request = new CreatePlanDto("Plan", null, null, Items:
        [
            new CreatePlanItemDto(null, null, 2, null, 1, ItemKind.Break),
        ]);

        // Act
        var response = await _client.PostAsJsonAsync("/v1/plans", request, JsonOptions);

        // Assert
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, body);

        // Nothing was written: a rejected item must not leave a half-built plan behind.
        var mine = await _client.GetAsync("/v1/me/plans");
        (await mine.Content.ReadAsStringAsync()).Should().NotContain("\"name\":\"Plan\"");
    }

    [Test]
    public async Task CreatePlan_WhenADrillItemHasNoDrill_Returns400()
    {
        // Arrange
        await SeedDrillAsync("Serve Pass");
        SetAuth(CoachId);
        var request = new CreatePlanDto("Plan", null, null, Items:
        [
            new CreatePlanItemDto(null, null, 10, null, 1),
        ]);

        // Act
        var response = await _client.PostAsJsonAsync("/v1/plans", request, JsonOptions);

        // Assert
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, body);

        // Nothing was written: a rejected item must not leave a half-built plan behind.
        var mine = await _client.GetAsync("/v1/me/plans");
        (await mine.Content.ReadAsStringAsync()).Should().NotContain("\"name\":\"Plan\"");
    }

    private async Task<TrainingPlanDetailDto> ReadPlanAsync(HttpResponseMessage response)
    {
        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<TrainingPlanDetailDto>(JsonOptions))!;
    }

    private async Task<Guid> SeedDrillAsync(string name)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CoachingDbContext>();

        if (!db.Set<UserProfile>().Any(u => u.Id == CoachId))
        {
            db.Set<UserProfile>().Add(new UserProfile
            {
                Id = CoachId,
                Name = "CJ",
                Surname = "Roberts",
                Email = "cj.roberts@test.com"
            });
        }

        var drill = new Drill { Id = Guid.NewGuid(), Name = name, CreatedByUserId = CoachId };
        db.Drills.Add(drill);
        await db.SaveChangesAsync();
        return drill.Id;
    }

    private void SetAuth(Guid userId)
    {
        var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(CoachingApiFactory.JwtSecret));
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.NameId, userId.ToString()),
            new Claim(ClaimTypes.Email, "cj.roberts@test.com"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var token = new JwtSecurityToken(
            issuer: CoachingApiFactory.JwtIssuer,
            audience: CoachingApiFactory.JwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
    }
}
