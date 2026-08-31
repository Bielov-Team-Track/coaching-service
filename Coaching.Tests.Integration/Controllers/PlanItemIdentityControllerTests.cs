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
using Coaching.Domain.Models.Templates;
using Coaching.Infrastructure.Data.Context;
using Coaching.Tests.Integration.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Shared.Models;

namespace Coaching.Tests.Integration.Controllers;

/// <summary>
/// A save keeps the rows it is given back. The unit tests hold the reconcile's shape; these
/// hold what only a real database can answer — that one SaveChangesAsync orders the inserts,
/// updates and deletes it stages so the foreign keys between them are never violated, and that
/// a coach assignment hanging off a station id is still there afterwards.
/// </summary>
[TestFixture]
[Category("Integration")]
public class PlanItemIdentityControllerTests
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
    public async Task UpdatePlan_ResendingEveryId_KeepsTheRowsAndTheStaffingOnThem()
    {
        // Arrange — a section, a drill row answering two dials, and a Stations block of two
        // groups. One group is staffed, the way a lead coach leaves it before the session.
        var drillId = await SeedDrillAsync("Serve Pass");
        SetAuth(CoachId);

        var sectionId = Guid.NewGuid();
        var create = new CreatePlanDto("Wed 18 Jun", null, null,
            Sections: [new CreatePlanSectionDto("Warm-up", 0, sectionId)],
            Items:
            [
                new CreatePlanItemDto(drillId, sectionId, 10, null, 1,
                    DialValues: new Dictionary<string, string> { ["reps"] = "12", ["tempo"] = "fast" }),
                new CreatePlanItemDto(null, null, 20, null, 2, ItemKind.Stations, "Stations", 20,
                [
                    new CreatePlanStationDto("Setters", 0, [new CreatePlanStationItemDto(drillId, 10, null, 0)]),
                    new CreatePlanStationDto("Hitters", 1, [new CreatePlanStationItemDto(drillId, 20, null, 0)]),
                ]),
            ]);

        var plan = await ReadPlanAsync(await _client.PostAsJsonAsync("/v1/plans", create, JsonOptions));
        var drillRow = plan.Items.Single(i => i.Kind == ItemKind.Drill);
        var block = plan.Items.Single(i => i.Kind == ItemKind.Stations);
        var setters = block.Stations.Single(s => s.Name == "Setters");
        var hitters = block.Stations.Single(s => s.Name == "Hitters");
        var assignedCoachId = await StaffAsync(setters.Id);

        // Act — the plan comes back with every id on it: the section renamed, the two rows
        // swapped, one dial answer changed and the other dropped, and a water break added to a
        // group under an id the client minted for it.
        var breakRowId = Guid.NewGuid();
        var update = new UpdatePlanDto(null, null, null, null, null,
            [new CreatePlanSectionDto("Warm-up and serve", 0, sectionId)],
            [
                new CreatePlanItemDto(null, null, 20, null, 1, ItemKind.Stations, "Stations", 20,
                [
                    new CreatePlanStationDto("Setters", 0,
                    [
                        new CreatePlanStationItemDto(drillId, 10, null, 0, Id: setters.Items[0].Id),
                        new CreatePlanStationItemDto(null, 5, null, 1, ItemKind.Break, "Water", Id: breakRowId),
                    ], setters.Id),
                    new CreatePlanStationDto("Hitters", 1,
                        [new CreatePlanStationItemDto(drillId, 20, null, 0, Id: hitters.Items[0].Id)], hitters.Id),
                ], Id: block.Id),
                new CreatePlanItemDto(drillId, sectionId, 15, null, 2, Id: drillRow.Id,
                    DialValues: new Dictionary<string, string> { ["reps"] = "8" }),
            ]);

        var response = await _client.PutAsJsonAsync($"/v1/plans/{plan.Id}", update, JsonOptions);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        // Assert
        var reread = await ReadPlanAsync(await _client.GetAsync($"/v1/plans/{plan.Id}"));

        reread.Sections.Should().ContainSingle();
        reread.Sections[0].Id.Should().Be(sectionId);
        reread.Sections[0].Name.Should().Be("Warm-up and serve");

        var keptRow = reread.Items.Single(i => i.Kind == ItemKind.Drill);
        keptRow.Id.Should().Be(drillRow.Id);
        keptRow.Order.Should().Be(2);
        keptRow.Duration.Should().Be(15);
        keptRow.SectionId.Should().Be(sectionId);
        keptRow.DialValues.Should().Equal(new Dictionary<string, string> { ["reps"] = "8" });

        var keptBlock = reread.Items.Single(i => i.Kind == ItemKind.Stations);
        keptBlock.Id.Should().Be(block.Id);
        keptBlock.Order.Should().Be(1);
        keptBlock.Stations.Select(s => s.Id).Should().BeEquivalentTo(new[] { setters.Id, hitters.Id });

        // The point of the whole slice: the assignment hangs off the station id and nothing
        // else, so a station that survived the save is still staffed.
        var keptSetters = keptBlock.Stations.Single(s => s.Id == setters.Id);
        keptSetters.Coaches.Should().ContainSingle().Which.UserId.Should().Be(assignedCoachId);

        keptSetters.Items.Should().HaveCount(2);
        keptSetters.Items[0].Id.Should().Be(setters.Items[0].Id);
        keptSetters.Items[1].Id.Should().Be(breakRowId);
        keptSetters.Items[1].Kind.Should().Be(ItemKind.Break);
    }

    [Test]
    public async Task UpdatePlan_MovingARowIntoASectionTheSameSaveCreates_Works()
    {
        // Arrange — one save inserts a section, updates a surviving row to point at it, and
        // deletes the section that row was in. All three land in one SaveChangesAsync, so the
        // database is the only thing that can say the order was legal.
        var drillId = await SeedDrillAsync("Serve Pass");
        SetAuth(CoachId);

        var oldSectionId = Guid.NewGuid();
        var plan = await ReadPlanAsync(await _client.PostAsJsonAsync("/v1/plans",
            new CreatePlanDto("Plan", null, null,
                Sections: [new CreatePlanSectionDto("Warm-up", 0, oldSectionId)],
                Items: [new CreatePlanItemDto(drillId, oldSectionId, 10, null, 1)]),
            JsonOptions));

        var itemId = plan.Items.Single().Id;
        var newSectionId = Guid.NewGuid();

        // Act
        var response = await _client.PutAsJsonAsync($"/v1/plans/{plan.Id}",
            new UpdatePlanDto(null, null, null, null, null,
                [new CreatePlanSectionDto("Main set", 0, newSectionId)],
                [new CreatePlanItemDto(drillId, newSectionId, 10, null, 1, Id: itemId)]),
            JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var reread = await ReadPlanAsync(await _client.GetAsync($"/v1/plans/{plan.Id}"));
        reread.Sections.Should().ContainSingle().Which.Id.Should().Be(newSectionId);
        reread.Items.Should().ContainSingle();
        reread.Items[0].Id.Should().Be(itemId);
        reread.Items[0].SectionId.Should().Be(newSectionId);
    }

    [Test]
    public async Task UpdatePlan_WithAnItemIdFromAnotherPlan_Returns400()
    {
        // Arrange — honouring it would build an insert that collides on the primary key
        var drillId = await SeedDrillAsync("Serve Pass");
        SetAuth(CoachId);

        var theirs = await ReadPlanAsync(await _client.PostAsJsonAsync("/v1/plans",
            new CreatePlanDto("Theirs", null, null, Items: [new CreatePlanItemDto(drillId, null, 10, null, 1)]),
            JsonOptions));
        var mine = await ReadPlanAsync(await _client.PostAsJsonAsync("/v1/plans",
            new CreatePlanDto("Mine", null, null, Items: [new CreatePlanItemDto(drillId, null, 10, null, 1)]),
            JsonOptions));

        // Act
        var response = await _client.PutAsJsonAsync($"/v1/plans/{mine.Id}",
            new UpdatePlanDto(null, null, null, null, null, null,
                [new CreatePlanItemDto(drillId, null, 30, null, 1, Id: theirs.Items.Single().Id)]),
            JsonOptions);

        // Assert — and the other plan is untouched
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, await response.Content.ReadAsStringAsync());

        var theirsAgain = await ReadPlanAsync(await _client.GetAsync($"/v1/plans/{theirs.Id}"));
        theirsAgain.Items.Single().Duration.Should().Be(10);
    }

    private async Task<TrainingPlanDetailDto> ReadPlanAsync(HttpResponseMessage response)
    {
        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<TrainingPlanDetailDto>(JsonOptions))!;
    }

    /// <summary>
    /// Writes the assignment straight to the table the coach endpoint writes: what is under test
    /// is that a plan save leaves the row alone, not how it came to be there.
    /// </summary>
    private async Task<Guid> StaffAsync(Guid stationId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CoachingDbContext>();

        var coachId = Guid.NewGuid();
        db.Set<PlanStationCoach>().Add(new PlanStationCoach { StationId = stationId, UserId = coachId });
        await db.SaveChangesAsync();
        return coachId;
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
