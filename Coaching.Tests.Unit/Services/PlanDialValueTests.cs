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
/// A drill's dials are answered per use, and the answers ride inside the item that carries
/// them: saving a plan deletes and recreates every item, so anything sent alongside would be
/// lost on the first save. These tests hold that shape — for a row on the spine and a row
/// inside a station group alike, because a build that only ever reaches the spine passes
/// every single-row test.
/// </summary>
[TestFixture]
[Category("Unit")]
public class PlanDialValueTests : UnitTestBase
{
    private IPlanItemRepository _itemRepository = null!;
    private ITrainingPlanRepository _planRepository = null!;
    private IDrillRepository _drillRepository = null!;
    private IRepository<PlanItemDialValue> _dialValueRepository = null!;
    private IMapper _mapper = null!;
    private TrainingPlanService _sut = null!;

    private readonly List<PlanItem> _addedItems = [];
    private readonly List<PlanItemDialValue> _addedValues = [];
    private readonly List<PlanItemDialValue> _storedValues = [];

    private Guid _userId;
    private Guid _drillId;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();

        _addedItems.Clear();
        _addedValues.Clear();
        _storedValues.Clear();

        _userId = Guid.NewGuid();
        _drillId = Guid.NewGuid();

        _itemRepository = Substitute.For<IPlanItemRepository>();
        _planRepository = Substitute.For<ITrainingPlanRepository>();
        _drillRepository = Substitute.For<IDrillRepository>();
        _dialValueRepository = Substitute.For<IRepository<PlanItemDialValue>>();

        _drillRepository.GetByIdAsync(_drillId).Returns(new Drill { Id = _drillId, Name = "Serve receive" });
        _itemRepository.When(r => r.Add(Arg.Any<PlanItem>())).Do(c => _addedItems.Add(c.Arg<PlanItem>()));
        _itemRepository.GetByTemplateAsync(Arg.Any<Guid>()).Returns(_ => _addedItems);

        _dialValueRepository.Query().Returns(_ => _storedValues.BuildMock());
        _dialValueRepository.When(r => r.Add(Arg.Any<PlanItemDialValue>())).Do(c => _addedValues.Add(c.Arg<PlanItemDialValue>()));

        _mapper = Substitute.For<IMapper>();
        _mapper.Map<TrainingPlanDetailDto>(Arg.Any<TrainingPlan?>()).Returns(new TrainingPlanDetailDto { Name = "plan" });

        _sut = new TrainingPlanService(
            _planRepository,
            Substitute.For<IPlanSectionRepository>(),
            _itemRepository,
            Substitute.For<IPlanLikeRepository>(),
            Substitute.For<IPlanBookmarkRepository>(),
            Substitute.For<IPlanCommentRepository>(),
            _drillRepository,
            _dialValueRepository,
            Substitute.For<IRepository<PlanStation>>(),
            Substitute.For<IRepository<PlanStationItem>>(),
            Substitute.For<IClubsGrpcClient>(),
            Substitute.For<IEventsGrpcClient>(),
            Substitute.For<IPlanCoachService>(),
            Substitute.For<IPublishEndpoint>(),
            _mapper,
            Substitute.For<ILogger<TrainingPlanService>>());
    }

    [Test]
    public async Task CreateAsync_RecordsWhatASpineRowSetItsDialsTo()
    {
        // Arrange — two dials on one row, so a write that only handles the first is caught
        var row = DrillRow(20, new() { ["reps"] = "12", ["tempo"] = "fast" });

        // Act
        await CreateWith(row);

        // Assert
        var item = _addedItems.Single();
        _addedValues.Should().HaveCount(2);
        _addedValues.Should().OnlyContain(v => v.ItemId == item.Id && v.StationItemId == null);
        _addedValues.Single(v => v.DialName == "reps").Value.Should().Be("12");
        _addedValues.Single(v => v.DialName == "tempo").Value.Should().Be("fast");
    }

    [Test]
    public async Task CreateAsync_RecordsWhatAGroupRowSetItsDialsTo()
    {
        // Arrange — two groups, each with a row answering the same dial differently
        var stations = Stations(
            20,
            new CreatePlanStationDto("Setters", 0, [GroupRow(20, new() { ["reps"] = "12" })]),
            new CreatePlanStationDto("Hitters", 1, [GroupRow(20, new() { ["reps"] = "6" })]));

        // Act
        await CreateWith(stations);

        // Assert
        var groupRowIds = _addedItems.Single().Stations.SelectMany(s => s.Items).Select(r => r.Id).ToList();
        _addedValues.Should().HaveCount(2);
        _addedValues.Should().OnlyContain(v => v.ItemId == null && v.StationItemId != null);
        _addedValues.Select(v => v.StationItemId!.Value).Should().BeEquivalentTo(groupRowIds);
        _addedValues.Select(v => v.Value).Should().BeEquivalentTo(new[] { "12", "6" });
    }

    [Test]
    public async Task CreateAsync_KeepsTheSameDrillsTwoRowsAnsweringDifferently()
    {
        // Arrange — the whole point of a dial: one library drill, two readings in one plan
        await CreateWith(
            DrillRow(20, new() { ["reps"] = "12" }),
            DrillRow(15, new() { ["reps"] = "6" }));

        // Assert
        _addedValues.Should().HaveCount(2);
        _addedValues.Select(v => v.ItemId).Should().OnlyHaveUniqueItems();
        _addedValues.Select(v => v.Value).Should().BeEquivalentTo(new[] { "12", "6" });
    }

    [Test]
    public async Task CreateAsync_KeepsAnAnswerNoDialGoesByAnyMore()
    {
        // Arrange — a dial the drill has since dropped. Throwing the answer away would lose the
        // coach's work for good if the dial ever came back.
        await CreateWith(DrillRow(20, new() { ["retired"] = "still here" }));

        // Assert
        _addedValues.Single().DialName.Should().Be("retired");
    }

    [Test]
    public async Task CreateAsync_WhenAnAnswerIsLongerThanTheColumn_Throws()
    {
        // Arrange — one over-long answer must not take the whole plan down mid-save
        var row = DrillRow(20, new() { ["reps"] = new string('x', PlanItemDialValue.ValueMaxLength + 1) });

        // Act
        var act = () => CreateWith(row);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Test]
    public async Task CreateAsync_WhenARowAnswersNothing_WritesNoRows()
    {
        await CreateWith(DrillRow(20, null));

        _addedValues.Should().BeEmpty();
    }

    [Test]
    public async Task GetByIdAsync_FillsEachRowsAnswersFromThePlansOwnRecords()
    {
        // Arrange — the values are stored against the plan, not the items, so the read has to
        // hand them back to the right row on both sides of a Stations block
        var plan = new TrainingPlan { Name = "Plan", Visibility = TemplateVisibility.Public, CreatedByUserId = _userId };
        var spineId = Guid.NewGuid();
        var groupRowId = Guid.NewGuid();

        _planRepository.GetByIdWithDetailsAsync(plan.Id).Returns(plan);
        _mapper.Map<TrainingPlanDetailDto>(Arg.Any<TrainingPlan?>()).Returns(new TrainingPlanDetailDto
        {
            Id = plan.Id,
            Name = "Plan",
            Items =
            [
                new PlanItemDto { Id = spineId },
                new PlanItemDto
                {
                    Id = Guid.NewGuid(),
                    Kind = ItemKind.Stations,
                    Stations = [new PlanStationDto { Name = "Setters", Items = [new PlanStationItemDto { Id = groupRowId }] }],
                },
            ],
        });

        _storedValues.Add(new PlanItemDialValue { PlanId = plan.Id, ItemId = spineId, DialName = "reps", Value = "12" });
        _storedValues.Add(new PlanItemDialValue { PlanId = plan.Id, StationItemId = groupRowId, DialName = "reps", Value = "6" });
        _storedValues.Add(new PlanItemDialValue { PlanId = Guid.NewGuid(), ItemId = spineId, DialName = "reps", Value = "99" });

        // Act
        var result = await _sut.GetByIdAsync(plan.Id, _userId);

        // Assert
        result!.Items[0].DialValues.Should().Contain("reps", "12");
        result.Items[1].Stations[0].Items[0].DialValues.Should().Contain("reps", "6");
    }

    private CreatePlanItemDto DrillRow(int duration, Dictionary<string, string>? dialValues) =>
        new(_drillId, null, duration, null, DialValues: dialValues);

    private CreatePlanStationItemDto GroupRow(int duration, Dictionary<string, string>? dialValues) =>
        new(_drillId, duration, null, 0, DialValues: dialValues);

    private static CreatePlanItemDto Stations(int duration, params CreatePlanStationDto[] groups) =>
        new(null, null, duration, null, null, ItemKind.Stations, "Stations", null, groups.ToList());

    private Task<TrainingPlanDetailDto> CreateWith(params CreatePlanItemDto[] items) =>
        _sut.CreateAsync(new CreatePlanDto("Plan", null, null, Items: items.ToList()), _userId);
}
