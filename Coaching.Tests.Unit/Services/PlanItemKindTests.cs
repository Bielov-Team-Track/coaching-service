using AutoMapper;
using Coaching.Application.DTOs.Templates;
using Coaching.Application.Interfaces.Repositories;
using Coaching.Application.Interfaces.Services;
using Coaching.Application.Mappings;
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
/// A break is its own kind of row, not a drill with a break-shaped name. These cover the
/// three things that follow from that: it carries no drill, it is not coached time, and a
/// goal set on one use overrides the drill's own without replacing it.
/// </summary>
[TestFixture]
[Category("Unit")]
public class PlanItemKindTests : UnitTestBase
{
    private ITrainingPlanRepository _planRepository = null!;
    private IPlanItemRepository _itemRepository = null!;
    private IDrillRepository _drillRepository = null!;
    private readonly List<PlanItem> _added = [];
    private TrainingPlanService _sut = null!;
    private Guid _userId;
    private Guid _drillId;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _added.Clear();
        _userId = Guid.NewGuid();
        _drillId = Guid.NewGuid();

        _planRepository = Substitute.For<ITrainingPlanRepository>();
        _itemRepository = Substitute.For<IPlanItemRepository>();
        _drillRepository = Substitute.For<IDrillRepository>();

        _drillRepository.GetByIdAsync(_drillId).Returns(new Drill { Id = _drillId, Name = "BSBH" });

        // The service adds items then re-reads them to recalculate the totals; the fake has to
        // behave like a store or the duration assertions test nothing.
        _itemRepository.When(r => r.Add(Arg.Any<PlanItem>())).Do(c => _added.Add(c.Arg<PlanItem>()));
        _itemRepository.GetByTemplateAsync(Arg.Any<Guid>()).Returns(_ => _added);

        var mapper = Substitute.For<IMapper>();
        mapper.Map<TrainingPlanDetailDto>(Arg.Any<TrainingPlan?>()).Returns(new TrainingPlanDetailDto { Name = "plan" });

        _sut = new TrainingPlanService(
            _planRepository,
            Substitute.For<IPlanSectionRepository>(),
            _itemRepository,
            Substitute.For<IPlanLikeRepository>(),
            Substitute.For<IPlanBookmarkRepository>(),
            Substitute.For<IPlanCommentRepository>(),
            _drillRepository,
            Substitute.For<IClubsGrpcClient>(),
            Substitute.For<IEventsGrpcClient>(),
            Substitute.For<IPlanCoachService>(),
            Substitute.For<IPublishEndpoint>(),
            mapper,
            Substitute.For<ILogger<TrainingPlanService>>());
    }

    private CreatePlanItemDto Drill(int duration = 10) =>
        new(_drillId, null, duration, null, null, ItemKind.Drill);

    private static CreatePlanItemDto Break(int duration = 2, string title = "Water") =>
        new(null, null, duration, null, null, ItemKind.Break, title);

    private static CreatePlanItemDto Meeting(int duration = 5, string title = "Breakout") =>
        new(null, null, duration, null, null, ItemKind.Meeting, title);

    private Task<TrainingPlanDetailDto> CreateWith(params CreatePlanItemDto[] items) =>
        _sut.CreateAsync(new CreatePlanDto("Plan", null, null, Items: items.ToList()), _userId);

    [Test]
    public async Task CreateAsync_WhenItemIsABreak_StoresItWithNoDrill()
    {
        // Act — her "Water and Serve" splits into a break and an ordinary drill
        await CreateWith(Break(), Drill());

        // Assert
        var stored = _added.Single(i => i.Kind == ItemKind.Break);
        stored.DrillId.Should().BeNull();
        stored.Title.Should().Be("Water");
    }

    [Test]
    public async Task CreateAsync_WhenBreakCarriesADrillId_DropsIt()
    {
        // Arrange — a client that sends a stale drill id on a break must not have it stored
        var contradictory = new CreatePlanItemDto(_drillId, null, 2, null, null, ItemKind.Break, "Water");

        // Act
        await CreateWith(contradictory);

        // Assert
        _added.Single().DrillId.Should().BeNull();
    }

    [Test]
    public async Task CreateAsync_WhenDrillItemHasNoDrill_Throws()
    {
        // Arrange
        var drillWithoutDrill = new CreatePlanItemDto(null, null, 10, null, null, ItemKind.Drill);

        // Act
        var act = () => CreateWith(drillWithoutDrill);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Test]
    public async Task CreateAsync_WhenBreakHasNoTitle_Throws()
    {
        // Act
        var act = () => CreateWith(Break(title: "  "));

        // Assert
        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Test]
    public async Task CreateAsync_WhenPlanHasBreaks_CoachedDurationExcludesThem()
    {
        // Arrange — 10 + 2 + 5 + 15: two of those are not coaching
        TrainingPlan? saved = null;
        _planRepository.When(r => r.Add(Arg.Any<TrainingPlan>())).Do(c => saved = c.Arg<TrainingPlan>());
        _planRepository.GetByIdAsync(Arg.Any<Guid>()).Returns(_ => saved);

        // Act
        await CreateWith(Drill(10), Break(2), Meeting(5), Drill(15));

        // Assert
        saved!.TotalDuration.Should().Be(32);
        saved.CoachedDuration.Should().Be(25);
    }

    [Test]
    public async Task CreateAsync_ForANonDrillKind_NeverLooksUpADrill()
    {
        // Act
        await CreateWith(Break(), Meeting());

        // Assert — a break has no drill to validate, so the library is never touched
        await _drillRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>());
    }

    [TestCase(ItemKind.Drill, true, true)]
    [TestCase(ItemKind.Stations, true, false)]
    [TestCase(ItemKind.Break, false, false)]
    [TestCase(ItemKind.Meeting, false, false)]
    public void ItemKinds_AnswerCoachedAndHasDrill(ItemKind kind, bool coached, bool hasDrill)
    {
        kind.IsCoached().Should().Be(coached);
        kind.HasDrill().Should().Be(hasDrill);
    }
}
