using Coaching.Domain.Enums;
using Coaching.Domain.Models.Feedback;
using Coaching.Infrastructure.Data.Context;
using Coaching.Infrastructure.Repositories;
using Coaching.Tests.Integration.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Coaching.Tests.Integration.Repositories;

/// <summary>
/// The feedback lists eager-load two collections on every row. Loaded in one statement that is a
/// Cartesian product — every improvement point against every attachment — and it grows with both
/// (SPI-5359). These read the SQL actually sent to Postgres, because the shape of the query is the
/// whole change: the rows come back identical either way.
/// </summary>
[TestFixture]
[Category("Integration")]
public class FeedbackListQueryTests
{
    private const string ImprovementPointsTable = "\"ImprovementPoints\"";
    private const string MediaTable = "\"FeedbackMedia\"";

    private CoachingApiFactory _factory = null!;
    private string _connectionString = null!;

    public delegate Task<IEnumerable<Feedback>> ListQuery(FeedbackRepository repository, Feedback seeded);

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _factory = new CoachingApiFactory();
        await _factory.InitializeAsync();

        using var scope = _factory.Services.CreateScope();
        _connectionString = scope.ServiceProvider
            .GetRequiredService<CoachingDbContext>()
            .Database.GetConnectionString()!;
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown() => await _factory.DisposeAsync();

    [TearDown]
    public async Task TearDown() => await _factory.DatabaseResetter.ResetAsync();

    private static IEnumerable<TestCaseData> ListQueries()
    {
        yield return Case("ByRecipient", (repo, f) => repo.GetByRecipientIdAsync(f.RecipientUserId));
        yield return Case("ByCoach", (repo, f) => repo.GetByCoachIdAsync(f.CoachUserId));
        yield return Case("ByEvent", (repo, f) => repo.GetByEventIdAsync(f.EventId!.Value));
    }

    private static TestCaseData Case(string name, ListQuery query) =>
        new TestCaseData(query).SetName($"{{m}}_{name}");

    [TestCaseSource(nameof(ListQueries))]
    public async Task ListQuery_LoadingBothCollections_JoinsNeitherAgainstTheOther(ListQuery query)
    {
        // Arrange
        var seeded = await SeedFeedbackAsync(improvementPoints: 2, attachments: 2);
        var commands = new List<string>();
        await using var context = NewLoggedContext(commands);

        // Act
        await query(new FeedbackRepository(context), seeded);

        // Assert
        commands.Should().NotBeEmpty();
        commands.Should().NotContain(
            sql => sql.Contains(ImprovementPointsTable) && sql.Contains(MediaTable),
            "the two collections multiply each other when one statement carries both");
    }

    [TestCaseSource(nameof(ListQueries))]
    public async Task ListQuery_SplitAcrossStatements_StillCarriesEveryChild(ListQuery query)
    {
        // Arrange
        var seeded = await SeedFeedbackAsync(improvementPoints: 2, attachments: 3);
        await using var context = NewLoggedContext([]);

        // Act
        var results = await query(new FeedbackRepository(context), seeded);

        // Assert
        var feedback = results.Should().ContainSingle().Subject;
        feedback.ImprovementPoints.Should().HaveCount(2);
        feedback.Media.Should().HaveCount(3);
        feedback.Praise.Should().NotBeNull();
    }

    private CoachingDbContext NewLoggedContext(List<string> commands) =>
        new(new DbContextOptionsBuilder<CoachingDbContext>()
            .UseNpgsql(_connectionString)
            .LogTo(commands.Add, [RelationalEventId.CommandExecuted])
            .Options);

    private async Task<Feedback> SeedFeedbackAsync(int improvementPoints, int attachments)
    {
        await using var context = NewLoggedContext([]);

        var feedback = new Feedback
        {
            RecipientUserId = Guid.NewGuid(),
            CoachUserId = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            SharedWithPlayer = true,
            Content = "Good session.",
            CreatedAt = DateTime.UtcNow,
            Praise = new Praise { Message = "Great serving.", BadgeType = BadgeType.Effort }
        };

        for (var i = 0; i < improvementPoints; i++)
            feedback.ImprovementPoints.Add(new ImprovementPoint { Description = $"Point {i}", Order = i });

        for (var i = 0; i < attachments; i++)
            feedback.Media.Add(new FeedbackMedia
            {
                Url = $"feedback/clip-{i}.mp4",
                Type = FeedbackMediaType.Video,
                Order = i
            });

        context.Feedbacks.Add(feedback);
        await context.SaveChangesAsync();
        return feedback;
    }
}
