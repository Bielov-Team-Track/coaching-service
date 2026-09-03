using Coaching.Application.Interfaces.Repositories;
using Coaching.Application.Interfaces.Services;
using Coaching.Application.Services;
using Coaching.Domain.Enums;
using Coaching.Domain.Models.Templates;
using FluentAssertions;
using MockQueryable;
using NSubstitute;
using Shared.Services.Analytics;
using Shared.Testing.Base;

namespace Coaching.Tests.Unit.Services;

/// <summary>
/// A run is the practice as it was started, not as the plan reads now: the plan can be edited —
/// or a Stations block deleted outright — while a coach is halfway through it. The run already
/// kept its own drill id for that reason; these are the groups, kept for the same one.
/// </summary>
[TestFixture]
[Category("Unit")]
public class RunStationTests : UnitTestBase
{
    private ITrainingPlanRunRepository _runRepository = null!;
    private ITrainingPlanRepository _planRepository = null!;
    private RunService _sut = null!;

    private static readonly Guid CreatorId = Guid.NewGuid();
    private static readonly Guid EventId = Guid.NewGuid();
    private static readonly Guid PlanId = Guid.NewGuid();
    private static readonly Guid BreakItemId = Guid.NewGuid();
    private static readonly Guid StationsItemId = Guid.NewGuid();
    private static readonly Guid SettersDrillId = Guid.NewGuid();
    private static readonly Guid HittersDrillId = Guid.NewGuid();

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _runRepository = Substitute.For<ITrainingPlanRunRepository>();
        _planRepository = Substitute.For<ITrainingPlanRepository>();
        _sut = new RunService(
            _runRepository,
            Substitute.For<ITrainingPlanRunItemRepository>(),
            Substitute.For<IRunStationRepository>(),
            _planRepository,
            Substitute.For<IRunBroadcaster>(),
            Substitute.For<IEventsGrpcClient>(),
            TimeProvider,
            Substitute.For<IAnalyticsCapture>());
    }

    /// <summary>
    /// A water break, then a Stations block split into two groups — the second of which takes
    /// its own water while the first keeps playing.
    /// </summary>
    private TrainingPlan BuildPlan() => new()
    {
        Id = PlanId,
        Name = "Tuesday session",
        CreatedByUserId = CreatorId,
        PlanType = PlanType.Instance,
        EventId = EventId,
        Items =
        [
            new PlanItem
            {
                Id = BreakItemId,
                TemplateId = PlanId,
                Kind = ItemKind.Break,
                Title = "Water",
                Order = 1,
                Duration = 5
            },
            new PlanItem
            {
                Id = StationsItemId,
                TemplateId = PlanId,
                Kind = ItemKind.Stations,
                Title = "Stations",
                Order = 2,
                Duration = 20,
                PlannedDuration = 20,
                Stations =
                [
                    new PlanStation
                    {
                        Name = "Setters",
                        Order = 0,
                        Items =
                        [
                            new PlanStationItem
                            {
                                Kind = ItemKind.Drill, DrillId = SettersDrillId, Order = 0, Duration = 20
                            }
                        ]
                    },
                    new PlanStation
                    {
                        Name = "Hitters",
                        Order = 1,
                        Items =
                        [
                            new PlanStationItem
                            {
                                Kind = ItemKind.Drill, DrillId = HittersDrillId, Order = 0, Duration = 12,
                                Notes = "Approach"
                            },
                            new PlanStationItem
                            {
                                Kind = ItemKind.Break, Title = "Water", Order = 1, Duration = 8
                            }
                        ]
                    }
                ]
            }
        ]
    };

    private void StubPlan(TrainingPlan plan) =>
        _planRepository.Query().Returns(_ => new List<TrainingPlan> { plan }.BuildMock());

    private void StubNoRun() =>
        _runRepository.GetByEventIdWithDetailsAsync(EventId).Returns((TrainingPlanRun?)null);

    private void StubExistingRun(TrainingPlanRun run)
    {
        _runRepository.GetByEventIdWithDetailsAsync(EventId).Returns(run);
        _runRepository.Query().Returns(_ => new List<TrainingPlanRun> { run }.BuildMock());
    }

    // ---------- First start ----------

    [Test]
    public async Task StartAsync_SnapshotsEveryGroupWithItsOwnRows()
    {
        // Arrange — two groups, not one: a snapshot that only ever reaches the first still
        // passes with a single group, which is how that shape survives review.
        StubPlan(BuildPlan());
        StubNoRun();

        // Act
        var result = await _sut.StartAsync(EventId, CreatorId);

        // Assert
        var stations = result.Items.Single(i => i.PlanItemId == StationsItemId).Stations;
        stations.Select(s => s.Name).Should().ContainInOrder("Setters", "Hitters");
        stations.Single(s => s.Name == "Setters").Items.Should().HaveCount(1);
        stations.Single(s => s.Name == "Hitters").Items.Should().HaveCount(2);
    }

    [Test]
    public async Task StartAsync_SnapshotsGroupLengthsInSeconds()
    {
        // Arrange — a run counts down; the plan is written in minutes.
        StubPlan(BuildPlan());
        StubNoRun();

        // Act
        var result = await _sut.StartAsync(EventId, CreatorId);

        // Assert
        var hitters = result.Items.Single(i => i.PlanItemId == StationsItemId)
            .Stations.Single(s => s.Name == "Hitters");
        hitters.Items.OrderBy(r => r.Order).Select(r => r.DurationSeconds).Should().Equal(720, 480);
    }

    [Test]
    public async Task StartAsync_SnapshotsABreakInsideAGroupAsABreakWithNoDrill()
    {
        // Arrange — one group takes water while the other keeps playing.
        StubPlan(BuildPlan());
        StubNoRun();

        // Act
        var result = await _sut.StartAsync(EventId, CreatorId);

        // Assert
        var row = result.Items.Single(i => i.PlanItemId == StationsItemId)
            .Stations.Single(s => s.Name == "Hitters").Items
            .Single(r => r.Kind == ItemKind.Break);
        row.Title.Should().Be("Water");
        row.DrillId.Should().BeNull();
    }

    [Test]
    public async Task StartAsync_CarriesTheKindAndTitleOfASpineRow()
    {
        // Arrange — the regression: every run item read as a drill, so a break on the run screen
        // was a drill row with no drill behind it.
        StubPlan(BuildPlan());
        StubNoRun();

        // Act
        var result = await _sut.StartAsync(EventId, CreatorId);

        // Assert
        var water = result.Items.Single(i => i.PlanItemId == BreakItemId);
        water.Kind.Should().Be(ItemKind.Break);
        water.Title.Should().Be("Water");
        water.DrillId.Should().BeNull();

        var stations = result.Items.Single(i => i.PlanItemId == StationsItemId);
        stations.Kind.Should().Be(ItemKind.Stations);
        stations.Title.Should().Be("Stations");
    }

    [Test]
    public async Task StartAsync_GivesTheSnapshotItsOwnIds()
    {
        // Arrange — the run owns its copy. Sharing ids with the plan would mean an edit to the
        // plan's groups reached back into a run that had already started.
        var plan = BuildPlan();
        StubPlan(plan);
        StubNoRun();
        var planStationIds = plan.Items.SelectMany(i => i.Stations).Select(s => s.Id).ToList();

        // Act
        var result = await _sut.StartAsync(EventId, CreatorId);

        // Assert
        result.Items.SelectMany(i => i.Stations).Select(s => s.Id)
            .Should().NotIntersectWith(planStationIds).And.OnlyHaveUniqueItems();
    }

    [Test]
    public async Task StartAsync_LeavesADrillRowWithNoGroups()
    {
        // Arrange
        StubPlan(BuildPlan());
        StubNoRun();

        // Act
        var result = await _sut.StartAsync(EventId, CreatorId);

        // Assert
        result.Items.Single(i => i.PlanItemId == BreakItemId).Stations.Should().BeEmpty();
    }

    // ---------- Restart ----------

    [Test]
    public async Task StartAsync_OnRestart_ReSnapshotsGroupsThatChangedInThePlan()
    {
        // Arrange — the coach reworked the block between runs: one group renamed, one dropped.
        var plan = BuildPlan();
        var stationsItem = plan.Items.Single(i => i.Id == StationsItemId);
        stationsItem.Stations = [stationsItem.Stations.First()];
        stationsItem.Stations.Single().Name = "Passers";
        StubPlan(plan);
        StubExistingRun(CompletedRunOfTheOriginalPlan());

        // Act
        var result = await _sut.StartAsync(EventId, CreatorId);

        // Assert
        result.Items.Single(i => i.PlanItemId == StationsItemId)
            .Stations.Select(s => s.Name).Should().Equal("Passers");
    }

    [Test]
    public async Task StartAsync_OnRestart_KeepsTheRunItemAndReplacesOnlyItsGroups()
    {
        // Arrange — the reconcile reuses the run item because its timings belong to the run.
        // Its groups hold no timings at all, so they are simply taken again.
        StubPlan(BuildPlan());
        var run = CompletedRunOfTheOriginalPlan();
        var keptRunItemId = run.Items.Single(i => i.PlanItemId == StationsItemId).Id;
        var oldStationIds = run.Items.SelectMany(i => i.Stations).Select(s => s.Id).ToList();
        StubExistingRun(run);

        // Act
        var result = await _sut.StartAsync(EventId, CreatorId);

        // Assert
        var stationsRunItem = result.Items.Single(i => i.PlanItemId == StationsItemId);
        stationsRunItem.Id.Should().Be(keptRunItemId);
        stationsRunItem.Stations.Should().HaveCount(2);
        stationsRunItem.Stations.Select(s => s.Id).Should().NotIntersectWith(oldStationIds);
    }

    [Test]
    public async Task StartAsync_OnRestart_RefreshesKindAndTitleOfAKeptRunItem()
    {
        // Arrange — the row was a drill last time round and is a break now.
        StubPlan(BuildPlan());
        var run = CompletedRunOfTheOriginalPlan();
        var water = run.Items.Single(i => i.PlanItemId == BreakItemId);
        water.Kind = ItemKind.Drill;
        water.Title = null;
        water.DrillId = SettersDrillId;
        StubExistingRun(run);

        // Act
        var result = await _sut.StartAsync(EventId, CreatorId);

        // Assert
        var refreshed = result.Items.Single(i => i.PlanItemId == BreakItemId);
        refreshed.Kind.Should().Be(ItemKind.Break);
        refreshed.Title.Should().Be("Water");
        refreshed.DrillId.Should().BeNull();
    }

    [Test]
    public async Task StartAsync_OnRestart_DropsTheGroupsOfARowThatIsNoLongerStations()
    {
        // Arrange — the coach replaced the block with a single drill; the groups it used to
        // hold must not survive as a split nothing draws.
        var plan = BuildPlan();
        var stationsItem = plan.Items.Single(i => i.Id == StationsItemId);
        stationsItem.Kind = ItemKind.Drill;
        stationsItem.Title = null;
        stationsItem.DrillId = SettersDrillId;
        stationsItem.Stations.Clear();
        StubPlan(plan);
        StubExistingRun(CompletedRunOfTheOriginalPlan());

        // Act
        var result = await _sut.StartAsync(EventId, CreatorId);

        // Assert
        var row = result.Items.Single(i => i.PlanItemId == StationsItemId);
        row.Kind.Should().Be(ItemKind.Drill);
        row.Stations.Should().BeEmpty();
    }

    /// <summary>A finished run of the plan as <see cref="BuildPlan"/> first described it.</summary>
    private TrainingPlanRun CompletedRunOfTheOriginalPlan()
    {
        var stationsRunItem = new TrainingPlanRunItem
        {
            PlanItemId = StationsItemId,
            Kind = ItemKind.Stations,
            Title = "Stations",
            Order = 2,
            PlannedDurationSeconds = 1200,
            Stations =
            [
                new RunStation
                {
                    Name = "Setters",
                    Order = 0,
                    Items = [new RunStationItem { Kind = ItemKind.Drill, DrillId = SettersDrillId, Order = 0, DurationSeconds = 1200 }]
                },
                new RunStation
                {
                    Name = "Hitters",
                    Order = 1,
                    Items = [new RunStationItem { Kind = ItemKind.Drill, DrillId = HittersDrillId, Order = 0, DurationSeconds = 720 }]
                }
            ]
        };

        return new TrainingPlanRun
        {
            PlanId = PlanId,
            EventId = EventId,
            StartedByUserId = CreatorId,
            Status = RunStatus.Completed,
            StartedAtUtc = PastDate(1),
            CompletedAtUtc = PastDate(1),
            Items =
            [
                new TrainingPlanRunItem
                {
                    PlanItemId = BreakItemId,
                    Kind = ItemKind.Break,
                    Title = "Water",
                    Order = 1,
                    PlannedDurationSeconds = 300
                },
                stationsRunItem
            ]
        };
    }
}
