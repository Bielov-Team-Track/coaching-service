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
using MockQueryable;
using NSubstitute;
using Shared.DataAccess.Repositories.Interfaces;
using Shared.Exceptions;
using Shared.Testing.Base;

namespace Coaching.Tests.Unit.Services;

/// <summary>
/// A row of a plan keeps its id across a save. Nothing that hangs off a row carries a foreign
/// key back to it — a station's coaches, a floor placement, a run's progress are all keyed to an
/// id alone — so recreating rows on every wizard save silently emptied every one of them. The
/// client sends the ids back and the save reconciles against them.
/// </summary>
[TestFixture]
[Category("Unit")]
public class PlanItemIdentityTests : UnitTestBase
{
    private ITrainingPlanRepository _planRepository = null!;
    private IPlanSectionRepository _sectionRepository = null!;
    private IPlanItemRepository _itemRepository = null!;
    private IRepository<PlanStation> _stationRepository = null!;
    private IRepository<PlanStationItem> _stationItemRepository = null!;
    private IRepository<PlanItemDialValue> _dialValueRepository = null!;
    private TrainingPlanService _sut = null!;

    private readonly List<PlanItemDialValue> _storedValues = [];
    private readonly List<PlanItem> _foreignItems = [];

    private TrainingPlan _plan = null!;
    private Guid _userId;
    private Guid _drillId;

    // The plan seeded for every test: one drill row inside a section, and a Stations block whose
    // first group is staffed. Two groups and two group rows on purpose — a reconcile that only
    // ever reaches the first element still passes every single-element test.
    private PlanSection _section = null!;
    private PlanItem _drillRow = null!;
    private PlanItem _stationsRow = null!;
    private PlanStation _setters = null!;
    private PlanStation _hitters = null!;
    private PlanStationItem _settersFirstRow = null!;
    private PlanStationItem _settersSecondRow = null!;
    private PlanStationCoach _settersCoach = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();

        _storedValues.Clear();
        _foreignItems.Clear();

        _userId = Guid.NewGuid();
        _drillId = Guid.NewGuid();

        SeedPlan();

        _planRepository = Substitute.For<ITrainingPlanRepository>();
        _sectionRepository = Substitute.For<IPlanSectionRepository>();
        _itemRepository = Substitute.For<IPlanItemRepository>();
        _stationRepository = Substitute.For<IRepository<PlanStation>>();
        _stationItemRepository = Substitute.For<IRepository<PlanStationItem>>();
        _dialValueRepository = Substitute.For<IRepository<PlanItemDialValue>>();

        _planRepository.GetByIdWithDetailsAsync(_plan.Id).Returns(_plan);
        _planRepository.GetByIdAsync(_plan.Id).Returns(_plan);
        _itemRepository.GetByTemplateAsync(_plan.Id).Returns(_ => _plan.Items.ToList());

        // The guard that rejects an id belonging to another plan reads these; only ids the plan
        // does not already own ever reach them.
        _sectionRepository.Query().Returns(_ => _plan.Sections.ToList().BuildMock());
        _itemRepository.Query().Returns(_ => _plan.Items.Concat(_foreignItems).ToList().BuildMock());
        _stationRepository.Query().Returns(_ => Stations().BuildMock());
        _stationItemRepository.Query().Returns(_ => Stations().SelectMany(st => st.Items).ToList().BuildMock());
        _dialValueRepository.Query().Returns(_ => _storedValues.BuildMock());

        var drillRepository = Substitute.For<IDrillRepository>();
        drillRepository.GetByIdAsync(_drillId).Returns(new Drill { Id = _drillId, Name = "Serve receive" });

        var mapper = Substitute.For<IMapper>();
        mapper.Map<TrainingPlanDetailDto>(Arg.Any<TrainingPlan?>()).Returns(new TrainingPlanDetailDto { Name = "plan" });

        _sut = new TrainingPlanService(
            _planRepository,
            _sectionRepository,
            _itemRepository,
            Substitute.For<IPlanLikeRepository>(),
            Substitute.For<IPlanBookmarkRepository>(),
            Substitute.For<IPlanCommentRepository>(),
            drillRepository,
            _dialValueRepository,
            _stationRepository,
            _stationItemRepository,
            Substitute.For<IClubsGrpcClient>(),
            Substitute.For<IEventsGrpcClient>(),
            Substitute.For<IPlanCoachService>(),
            Substitute.For<IPublishEndpoint>(),
            mapper,
            Substitute.For<ILogger<TrainingPlanService>>());
    }

    private void SeedPlan()
    {
        _plan = new TrainingPlan
        {
            Name = "Friday practice",
            CreatedByUserId = _userId,
            PlanType = PlanType.Template
        };

        _section = new PlanSection { TemplateId = _plan.Id, Name = "Warm-up", Order = 0 };

        _drillRow = new PlanItem
        {
            TemplateId = _plan.Id,
            Kind = ItemKind.Drill,
            DrillId = _drillId,
            SectionId = _section.Id,
            Duration = 20,
            Order = 1
        };

        _settersFirstRow = new PlanStationItem { Kind = ItemKind.Drill, DrillId = _drillId, Duration = 10, Order = 0 };
        _settersSecondRow = new PlanStationItem { Kind = ItemKind.Drill, DrillId = _drillId, Duration = 10, Order = 1 };
        _settersCoach = new PlanStationCoach { UserId = Guid.NewGuid() };

        _setters = new PlanStation { Name = "Setters", Order = 0 };
        _setters.Items.Add(_settersFirstRow);
        _setters.Items.Add(_settersSecondRow);
        _setters.Coaches.Add(_settersCoach);

        _hitters = new PlanStation { Name = "Hitters", Order = 1 };
        _hitters.Items.Add(new PlanStationItem { Kind = ItemKind.Drill, DrillId = _drillId, Duration = 20, Order = 0 });

        _stationsRow = new PlanItem
        {
            TemplateId = _plan.Id,
            Kind = ItemKind.Stations,
            Title = "Stations",
            Duration = 20,
            PlannedDuration = 20,
            Order = 2
        };
        _stationsRow.Stations.Add(_setters);
        _stationsRow.Stations.Add(_hitters);

        _plan.Sections.Add(_section);
        _plan.Items.Add(_drillRow);
        _plan.Items.Add(_stationsRow);

        _storedValues.Add(Value(itemId: _drillRow.Id, "reps", "12"));
        _storedValues.Add(Value(itemId: _drillRow.Id, "tempo", "fast"));
    }

    private PlanItemDialValue Value(Guid itemId, string dialName, string value) =>
        new() { PlanId = _plan.Id, ItemId = itemId, DialName = dialName, Value = value };

    private List<PlanStation> Stations() =>
        _plan.Items.SelectMany(i => i.Stations).ToList();

    // ---- payload builders -------------------------------------------------------------------

    private CreatePlanItemDto SameDrillRow(
        int duration = 20, int? order = null, Dictionary<string, string>? dialValues = null) =>
        new(_drillId, _section.Id, duration, null, order, Id: _drillRow.Id,
            DialValues: dialValues ?? new Dictionary<string, string> { ["reps"] = "12", ["tempo"] = "fast" });

    private CreatePlanItemDto SameStationsRow(params CreatePlanStationDto[] groups) =>
        new(null, null, 20, null, null, ItemKind.Stations, "Stations", 20, groups.ToList(), Id: _stationsRow.Id);

    private CreatePlanStationDto SameSetters(params CreatePlanStationItemDto[] rows) =>
        new("Setters", 0, rows.ToList(), _setters.Id);

    private CreatePlanStationDto SameHitters() =>
        new("Hitters", 1, [new CreatePlanStationItemDto(
            _drillId, 20, null, 0, Id: _hitters.Items.Single().Id)], _hitters.Id);

    private CreatePlanStationItemDto SameSettersFirstRow(int duration = 10, int order = 0) =>
        new(_drillId, duration, null, order, Id: _settersFirstRow.Id);

    private Task<TrainingPlanDetailDto> Save(
        List<CreatePlanItemDto>? items = null, List<CreatePlanSectionDto>? sections = null) =>
        _sut.UpdateAsync(_plan.Id, new UpdatePlanDto(null, null, null, null, null, sections, items), _userId);

    // ---- tests ------------------------------------------------------------------------------

    [Test]
    public async Task UpdateAsync_WhenAnItemIsResentByItsId_KeepsTheSameRow()
    {
        // Act — the same two rows come back, one of them with a changed length
        await Save([SameDrillRow(duration: 25), SameStationsRow(SameSetters(SameSettersFirstRow()))]);

        // Assert — updated in place, never dropped and rebuilt
        _itemRepository.Received(1).Update(_drillRow);
        _itemRepository.DidNotReceive().Delete(Arg.Any<PlanItem>());
        _itemRepository.DidNotReceive().Add(Arg.Any<PlanItem>());
        _plan.Items.Should().Contain(_drillRow);
        _drillRow.Duration.Should().Be(25);
    }

    [Test]
    public async Task UpdateAsync_WhenAGroupIsResentByItsId_KeepsTheCoachesOnIt()
    {
        // Arrange — the whole point of the slice: the lead coach staffed this group, and the
        // distribution must survive an edit of the practice it belongs to. Both groups come
        // back, with one of them re-timed, so nothing here is being dropped.
        var block = SameStationsRow(SameSetters(SameSettersFirstRow(duration: 15)), SameHitters());

        // Act
        await Save([SameDrillRow(), block]);

        // Assert — the row survived, so nothing wrote to its coaches
        _stationRepository.DidNotReceive().Delete(Arg.Any<PlanStation>());
        _stationRepository.DidNotReceive().Add(Arg.Any<PlanStation>());
        _stationsRow.Stations.Should().Contain(_setters);
        _setters.Coaches.Should().ContainSingle().Which.Should().BeSameAs(_settersCoach);
    }

    [Test]
    public async Task UpdateAsync_WhenAGroupIsNotResent_DropsItAndTheCoachesGoWithIt()
    {
        // Act — the practice keeps one group and loses the other
        await Save([SameDrillRow(), SameStationsRow(SameSetters(SameSettersFirstRow()))]);

        // Assert — the group the payload dropped goes; the one it kept stays staffed
        _stationRepository.Received(1).Delete(_hitters);
        _stationRepository.DidNotReceive().Delete(_setters);
        _stationsRow.Stations.Should().ContainSingle().Which.Should().BeSameAs(_setters);
    }

    [Test]
    public async Task UpdateAsync_WhenAGroupRowIsResentByItsId_KeepsItAndDropsTheOneThatIsNot()
    {
        // Act — the second row of the group is gone, the first stays
        await Save([SameDrillRow(), SameStationsRow(SameSetters(SameSettersFirstRow(duration: 12)))]);

        // Assert
        _stationItemRepository.Received(1).Update(_settersFirstRow);
        _stationItemRepository.Received(1).Delete(_settersSecondRow);
        _settersFirstRow.Duration.Should().Be(12);
        _setters.Items.Should().ContainSingle().Which.Should().BeSameAs(_settersFirstRow);
    }

    [Test]
    public async Task UpdateAsync_WhenAnItemIsNotResent_DeletesItsRow()
    {
        // Act — only the Stations block comes back
        await Save([SameStationsRow(SameSetters(SameSettersFirstRow()))]);

        // Assert
        _itemRepository.Received(1).Delete(_drillRow);
        _plan.Items.Should().NotContain(_drillRow);
    }

    [Test]
    public async Task UpdateAsync_WhenANewEntryCarriesAClientMintedId_CreatesTheRowWithThatId()
    {
        // Arrange — the wizard mints an id before the row has ever been saved, so that the
        // save after this one recognises the row rather than building a second copy of it.
        var mintedId = Guid.NewGuid();
        var added = new CreatePlanItemDto(_drillId, null, 12, null, Id: mintedId);

        // Act
        await Save([SameDrillRow(), SameStationsRow(SameSetters(SameSettersFirstRow())), added]);

        // Assert
        _itemRepository.Received(1).Add(Arg.Is<PlanItem>(i => i.Id == mintedId));
    }

    [Test]
    public async Task UpdateAsync_WhenANewEntryCarriesNoId_CreatesTheRowWithAServerId()
    {
        // Arrange
        var added = new CreatePlanItemDto(_drillId, null, 12, null);

        // Act
        await Save([SameDrillRow(), SameStationsRow(SameSetters(SameSettersFirstRow())), added]);

        // Assert
        _itemRepository.Received(1).Add(Arg.Is<PlanItem>(i => i.Id != Guid.Empty && i.Duration == 12));
    }

    [Test]
    public async Task UpdateAsync_WhenAnItemIdBelongsToAnotherPlan_Throws()
    {
        // Arrange — an id that exists but is not this plan's would be built as an insert and
        // collide on the primary key halfway through the save
        var stolen = new PlanItem { TemplateId = Guid.NewGuid(), Kind = ItemKind.Drill, DrillId = _drillId };
        _foreignItems.Add(stolen);

        // Act
        var act = () => Save([new CreatePlanItemDto(_drillId, null, 20, null, Id: stolen.Id)]);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>();
        _itemRepository.DidNotReceive().Add(Arg.Any<PlanItem>());
        _itemRepository.DidNotReceive().Delete(Arg.Any<PlanItem>());
    }

    [Test]
    public async Task UpdateAsync_WhenTheSameItemIdIsSentTwice_Throws()
    {
        // Arrange — the second entry would fold silently into the first and take its dial values
        var act = () => Save([SameDrillRow(), SameDrillRow(duration: 30)]);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>();
        _itemRepository.DidNotReceive().Delete(Arg.Any<PlanItem>());
    }

    [Test]
    public async Task UpdateAsync_WhenThePayloadCarriesNoIdsAtAll_ReplacesEverything()
    {
        // Arrange — the legacy shape, pinned: a client that has never heard of ids must still
        // get exactly what it got before, which is a plan rebuilt from the payload.
        var replacement = new CreatePlanItemDto(_drillId, null, 45, null);

        // Act
        await Save([replacement]);

        // Assert
        _itemRepository.Received(1).Delete(_drillRow);
        _itemRepository.Received(1).Delete(_stationsRow);
        _itemRepository.Received(1).Add(Arg.Is<PlanItem>(i => i.Duration == 45));
        _plan.Items.Should().ContainSingle().Which.Duration.Should().Be(45);
    }

    [Test]
    public async Task UpdateAsync_WhenItemsAreResentInANewOrder_RenumbersThemInPlace()
    {
        // Act — the two rows swap places, both still carrying their own ids
        await Save([SameStationsRow(SameSetters(SameSettersFirstRow())), SameDrillRow()]);

        // Assert
        _stationsRow.Order.Should().Be(1);
        _drillRow.Order.Should().Be(2);
        _itemRepository.DidNotReceive().Delete(Arg.Any<PlanItem>());
    }

    [Test]
    public async Task UpdateAsync_OnASurvivingItem_BringsItsDialValuesToWhatThePayloadSays()
    {
        // Arrange — one answer changes, one is dropped, one is new. Three at once because a
        // reconcile that only handles the changed case passes every single-dial test.
        var dials = new Dictionary<string, string> { ["reps"] = "8", ["tempo2"] = "slow" };

        // Act
        await Save([SameDrillRow(dialValues: dials), SameStationsRow(SameSetters(SameSettersFirstRow()))]);

        // Assert
        var reps = _storedValues.Single(v => v.DialName == "reps");
        reps.Value.Should().Be("8");
        _dialValueRepository.Received(1).Update(reps);
        _dialValueRepository.Received(1).Delete(_storedValues.Single(v => v.DialName == "tempo"));
        _dialValueRepository.Received(1).Add(Arg.Is<PlanItemDialValue>(
            v => v.DialName == "tempo2" && v.Value == "slow" && v.ItemId == _drillRow.Id));
    }

    [Test]
    public async Task UpdateAsync_WhenADialValueIsUnchanged_WritesNothingForIt()
    {
        // Act — the payload repeats what is already stored
        await Save([SameDrillRow(), SameStationsRow(SameSetters(SameSettersFirstRow()))]);

        // Assert
        _dialValueRepository.DidNotReceive().Update(Arg.Any<PlanItemDialValue>());
        _dialValueRepository.DidNotReceive().Add(Arg.Any<PlanItemDialValue>());
        _dialValueRepository.DidNotReceive().Delete(Arg.Any<PlanItemDialValue>());
    }

    [Test]
    public async Task UpdateAsync_WhenAnItemIsDeleted_TakesItsDialValuesWithIt()
    {
        // Arrange — the rows hold no key back to the item, so nothing else would take them
        // Act
        await Save([SameStationsRow(SameSetters(SameSettersFirstRow()))]);

        // Assert
        _dialValueRepository.Received(1).Delete(_storedValues.Single(v => v.DialName == "reps"));
        _dialValueRepository.Received(1).Delete(_storedValues.Single(v => v.DialName == "tempo"));
    }

    [Test]
    public async Task UpdateAsync_WhenASectionIsResentByItsId_KeepsTheSameRow()
    {
        // Act — renamed, same section
        await Save(
            items: [SameDrillRow(), SameStationsRow(SameSetters(SameSettersFirstRow()))],
            sections: [new CreatePlanSectionDto("Warm-up and serve", 0, _section.Id)]);

        // Assert — the item that lives in it never had to be repointed
        _sectionRepository.Received(1).Update(_section);
        _sectionRepository.DidNotReceive().Delete(Arg.Any<PlanSection>());
        _section.Name.Should().Be("Warm-up and serve");
        _drillRow.SectionId.Should().Be(_section.Id);
    }

    [Test]
    public async Task UpdateAsync_WhenASectionIsNotResent_DeletesIt()
    {
        // Act
        await Save(
            items: [SameDrillRow(), SameStationsRow(SameSetters(SameSettersFirstRow()))],
            sections: [new CreatePlanSectionDto("Serve receive", 0, Guid.NewGuid())]);

        // Assert
        _sectionRepository.Received(1).Delete(_section);
        _sectionRepository.Received(1).Add(Arg.Any<PlanSection>());
    }

    [Test]
    public async Task UpdateAsync_WhenOnlySectionsAreSent_LeavesTheItemsAlone()
    {
        // Arrange — the payload says nothing about the items, so it is not asking for them to go.
        // The old clear-and-recreate emptied the whole plan here.
        // Act
        await Save(sections: [new CreatePlanSectionDto("Warm-up", 0, _section.Id)]);

        // Assert
        _itemRepository.DidNotReceive().Delete(Arg.Any<PlanItem>());
        _plan.Items.Should().HaveCount(2);
    }

    [Test]
    public async Task UpdateAsync_WhenItemsAreSentAsAnEmptyList_ClearsThem()
    {
        // Arrange — the other half of the contract, and the one a client has to send to empty a
        // plan: an omitted array leaves the rows alone, an empty one asks for them to go.
        // Act
        await Save(items: []);

        // Assert
        _itemRepository.Received(1).Delete(_drillRow);
        _itemRepository.Received(1).Delete(_stationsRow);
        _plan.Items.Should().BeEmpty();
    }

    [Test]
    public async Task UpdateAsync_WhenANewGroupCarriesAClientMintedId_CreatesItAndItsRowWithThoseIds()
    {
        // Arrange — the ids the wizard will send for a group it has just added, so the save
        // after this one can staff the group without it having moved underneath.
        var groupId = Guid.NewGuid();
        var rowId = Guid.NewGuid();
        var added = new CreatePlanStationDto(
            "Liberos", 2, [new CreatePlanStationItemDto(_drillId, 20, null, 0, Id: rowId)], groupId);

        // Act
        await Save([
            SameDrillRow(),
            SameStationsRow(SameSetters(SameSettersFirstRow()), SameHitters(), added)
        ]);

        // Assert
        _stationRepository.Received(1).Add(Arg.Is<PlanStation>(st => st.Id == groupId && st.Order == 2));
        _stationItemRepository.Received(1).Add(Arg.Is<PlanStationItem>(r => r.Id == rowId && r.StationId == groupId));
    }
}
