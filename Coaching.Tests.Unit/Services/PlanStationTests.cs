using AutoMapper;
using Coaching.Application.DTOs.Templates;
using Coaching.Application.Interfaces.Repositories;
using Coaching.Application.Interfaces.Services;
using Coaching.Application.Services;
using Coaching.Domain.Enums;
using Coaching.Domain.Models.Drills;
using Coaching.Domain.Models.Templates;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shared.Exceptions;
using Shared.Testing.Base;

namespace Coaching.Tests.Unit.Services;

/// <summary>
/// A Stations row splits a stretch of the practice into groups running at the same time.
/// The groups are the row's, not the plan's: they go with it, they hold drills and breaks
/// but no further structure, and the row keeps the length the coach asked for separately
/// from the one the groups force on it.
/// </summary>
[TestFixture]
[Category("Unit")]
public class PlanStationTests : UnitTestBase
{
    private IPlanItemRepository _itemRepository = null!;
    private IDrillRepository _drillRepository = null!;
    private readonly List<PlanItem> _added = [];
    private TrainingPlanService _sut = null!;
    private Guid _userId;
    private Guid _drillId;
    private Guid _otherDrillId;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _added.Clear();
        _userId = Guid.NewGuid();
        _drillId = Guid.NewGuid();
        _otherDrillId = Guid.NewGuid();

        _itemRepository = Substitute.For<IPlanItemRepository>();
        _drillRepository = Substitute.For<IDrillRepository>();
        _drillRepository.GetByIdAsync(_drillId).Returns(new Drill { Id = _drillId, Name = "Serve receive" });
        _drillRepository.GetByIdAsync(_otherDrillId).Returns(new Drill { Id = _otherDrillId, Name = "Blocking" });

        _itemRepository.When(r => r.Add(Arg.Any<PlanItem>())).Do(c => _added.Add(c.Arg<PlanItem>()));
        _itemRepository.GetByTemplateAsync(Arg.Any<Guid>()).Returns(_ => _added);

        var mapper = Substitute.For<IMapper>();
        mapper.Map<TrainingPlanDetailDto>(Arg.Any<TrainingPlan?>()).Returns(new TrainingPlanDetailDto { Name = "plan" });

        _sut = new TrainingPlanService(
            Substitute.For<ITrainingPlanRepository>(),
            Substitute.For<IPlanSectionRepository>(),
            _itemRepository,
            Substitute.For<IPlanLikeRepository>(),
            Substitute.For<IPlanBookmarkRepository>(),
            Substitute.For<IPlanCommentRepository>(),
            _drillRepository,
            Substitute.For<IClubsGrpcClient>(),
            Substitute.For<IEventsGrpcClient>(),
            Substitute.For<IPublishEndpoint>(),
            mapper,
            Substitute.For<ILogger<TrainingPlanService>>());
    }

    private CreatePlanStationItemDto GroupDrill(Guid drillId, int duration, int order) =>
        new(drillId, duration, null, order);

    private CreatePlanItemDto Stations(int duration, int? planned, params CreatePlanStationDto[] groups) =>
        new(null, null, duration, null, null, ItemKind.Stations, "Stations", planned, groups.ToList());

    private Task<TrainingPlanDetailDto> CreateWith(params CreatePlanItemDto[] items) =>
        _sut.CreateAsync(new CreatePlanDto("Plan", null, null, Items: items.ToList()), _userId);

    [Test]
    public async Task CreateAsync_StoresEveryGroupWithItsOwnDrills()
    {
        // Arrange — two groups, not one: a build that only ever reaches the first still
        // passes with a single group, which is how that shape survives review.
        var stations = Stations(
            20,
            null,
            new CreatePlanStationDto("Setters", 0, [GroupDrill(_drillId, 20, 0)]),
            new CreatePlanStationDto("Hitters", 1, [GroupDrill(_otherDrillId, 12, 0), GroupDrill(_drillId, 8, 1)]));

        // Act
        await CreateWith(stations);

        // Assert
        var stored = _added.Single();
        stored.Stations.Should().HaveCount(2);
        stored.Stations.Last().Items.Should().HaveCount(2);
        stored.Stations.Last().Items.Last().DrillId.Should().Be(_drillId);
    }

    [Test]
    public async Task CreateAsync_KeepsTheGroupsInTheOrderTheyWereGiven()
    {
        // Arrange — the second group arrives first
        var stations = Stations(
            20,
            null,
            new CreatePlanStationDto("Hitters", 1, [GroupDrill(_drillId, 12, 0)]),
            new CreatePlanStationDto("Setters", 0, [GroupDrill(_drillId, 20, 0)]));

        // Act
        await CreateWith(stations);

        // Assert
        _added.Single().Stations.Select(s => s.Name).Should().ContainInOrder("Setters", "Hitters");
    }

    [Test]
    public async Task CreateAsync_WhenAGroupHoldsABreak_StoresItWithNoDrill()
    {
        // Arrange — one group takes water while the other keeps playing
        var withBreak = new CreatePlanStationDto(
            "Setters", 0, [new CreatePlanStationItemDto(null, 2, null, 0, ItemKind.Break, "Water")]);

        // Act
        await CreateWith(Stations(20, null, withBreak));

        // Assert
        var row = _added.Single().Stations.Single().Items.Single();
        row.Kind.Should().Be(ItemKind.Break);
        row.DrillId.Should().BeNull();
        row.Title.Should().Be("Water");
    }

    [Test]
    public async Task CreateAsync_KeepsThePlannedLengthApartFromTheOneTheGroupsForce()
    {
        // Act — the coach asked for 30 minutes; the longest group only fills 20
        await CreateWith(Stations(30, 30, new CreatePlanStationDto("Setters", 0, [GroupDrill(_drillId, 20, 0)])));

        // Assert
        _added.Single().PlannedDuration.Should().Be(30);
    }

    [Test]
    public async Task CreateAsync_WhenAGroupContainsStations_Throws()
    {
        // Arrange — stations inside stations has no meaning on a court
        var nested = new CreatePlanStationDto(
            "Setters", 0, [new CreatePlanStationItemDto(null, 10, null, 0, ItemKind.Stations, "Inner")]);

        // Act
        var act = () => CreateWith(Stations(20, null, nested));

        // Assert
        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Test]
    public async Task CreateAsync_WhenGroupsHangOffSomethingOtherThanStations_Throws()
    {
        // Arrange — a break with groups would store a split nothing draws
        var breakWithGroups = new CreatePlanItemDto(
            null, null, 2, null, null, ItemKind.Break, "Water", null,
            [new CreatePlanStationDto("Setters", 0, [GroupDrill(_drillId, 10, 0)])]);

        // Act
        var act = () => CreateWith(breakWithGroups);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Test]
    public async Task CreateAsync_WhenADrillInAGroupDoesNotExist_Throws()
    {
        // Arrange — the drills inside groups are checked like any other
        var missing = new CreatePlanStationDto("Setters", 0, [GroupDrill(Guid.NewGuid(), 10, 0)]);

        // Act
        var act = () => CreateWith(Stations(20, null, missing));

        // Assert
        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Test]
    public async Task CreateAsync_WhenAGroupHasNoName_Throws()
    {
        // Act
        var act = () => CreateWith(Stations(20, null, new CreatePlanStationDto("  ", 0, [GroupDrill(_drillId, 10, 0)])));

        // Assert
        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Test]
    public async Task CreateAsync_DoesNotCountAGroupsDrillsOnTopOfTheBlock()
    {
        // Arrange — the groups run inside the block's 20 minutes, not after them
        var stations = Stations(
            20,
            null,
            new CreatePlanStationDto("Setters", 0, [GroupDrill(_drillId, 20, 0)]),
            new CreatePlanStationDto("Hitters", 1, [GroupDrill(_otherDrillId, 15, 0)]));

        // Act
        await CreateWith(stations);

        // Assert — the plan is 20 minutes long, not 55
        _added.Sum(i => i.Duration).Should().Be(20);
    }
}
