using AutoMapper;
using Coaching.Application.DTOs.Templates;
using Coaching.Application.Interfaces.Repositories;
using Coaching.Application.Interfaces.Services;
using Coaching.Application.Services;
using Coaching.Domain.Enums;
using Coaching.Domain.Models.Templates;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using MockQueryable;
using NSubstitute;
using Shared.Testing.Base;

namespace Coaching.Tests.Unit.Services;

/// <summary>
/// Loading a template into an event and promoting an event plan back to a template are the same
/// copy in two directions, and a copy that drops half of a row is worse than no copy at all: the
/// coach gets a plan that looks like theirs and silently is not. A break arrived as a drill with
/// no drill behind it, and a Stations row arrived as an empty block.
/// </summary>
[TestFixture]
[Category("Unit")]
public class PlanCopyTests : UnitTestBase
{
    private ITrainingPlanRepository _planRepository = null!;
    private IPlanSectionRepository _sectionRepository = null!;
    private IPlanItemRepository _itemRepository = null!;
    private IEventsGrpcClient _eventsGrpcClient = null!;
    private readonly List<PlanItem> _copiedItems = [];
    private readonly List<PlanSection> _copiedSections = [];
    private TrainingPlanService _sut = null!;

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid SourcePlanId = Guid.NewGuid();
    private static readonly Guid SourceSectionId = Guid.NewGuid();
    private static readonly Guid DrillId = Guid.NewGuid();
    private static readonly Guid OtherDrillId = Guid.NewGuid();

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _copiedItems.Clear();
        _copiedSections.Clear();

        _planRepository = Substitute.For<ITrainingPlanRepository>();
        _sectionRepository = Substitute.For<IPlanSectionRepository>();
        _itemRepository = Substitute.For<IPlanItemRepository>();
        _eventsGrpcClient = Substitute.For<IEventsGrpcClient>();

        _itemRepository.When(r => r.Add(Arg.Any<PlanItem>())).Do(c => _copiedItems.Add(c.Arg<PlanItem>()));
        _sectionRepository.When(r => r.Add(Arg.Any<PlanSection>())).Do(c => _copiedSections.Add(c.Arg<PlanSection>()));
        _planRepository.Query().Returns(_ => new List<TrainingPlan>().BuildMock());

        var mapper = Substitute.For<IMapper>();
        mapper.Map<TrainingPlanDetailDto>(Arg.Any<TrainingPlan?>()).Returns(new TrainingPlanDetailDto { Name = "plan" });

        _sut = new TrainingPlanService(
            _planRepository,
            _sectionRepository,
            _itemRepository,
            Substitute.For<IPlanLikeRepository>(),
            Substitute.For<IPlanBookmarkRepository>(),
            Substitute.For<IPlanCommentRepository>(),
            Substitute.For<IDrillRepository>(),
            Substitute.For<IClubsGrpcClient>(),
            _eventsGrpcClient,
            Substitute.For<IPublishEndpoint>(),
            mapper,
            Substitute.For<ILogger<TrainingPlanService>>());
    }

    /// <summary>
    /// One warm-up drill in a section, a water break, and a Stations block split into two groups —
    /// the second of which takes its water while the first keeps playing.
    /// </summary>
    private TrainingPlan BuildSourcePlan(PlanType planType) => new()
    {
        Id = SourcePlanId,
        Name = "Tuesday session",
        CreatedByUserId = UserId,
        PlanType = planType,
        EventId = planType == PlanType.Instance ? Guid.NewGuid() : null,
        Sections = [new PlanSection { Id = SourceSectionId, TemplateId = SourcePlanId, Name = "Warm-up", Order = 1 }],
        Items =
        [
            new PlanItem
            {
                TemplateId = SourcePlanId,
                Kind = ItemKind.Drill,
                DrillId = DrillId,
                SectionId = SourceSectionId,
                Order = 1,
                Duration = 15,
                Notes = "Platform angle"
            },
            new PlanItem
            {
                TemplateId = SourcePlanId,
                Kind = ItemKind.Break,
                Title = "Water",
                Order = 2,
                Duration = 5,
                Notes = "Refill bottles"
            },
            new PlanItem
            {
                TemplateId = SourcePlanId,
                Kind = ItemKind.Stations,
                Title = "Stations",
                Order = 3,
                Duration = 20,
                PlannedDuration = 24,
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
                                Kind = ItemKind.Drill, DrillId = DrillId, Order = 0, Duration = 20, Notes = "Hands"
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
                                Kind = ItemKind.Drill, DrillId = OtherDrillId, Order = 0, Duration = 12
                            },
                            new PlanStationItem
                            {
                                Kind = ItemKind.Break, Title = "Water", Order = 1, Duration = 8, Notes = "Short one"
                            }
                        ]
                    }
                ]
            }
        ]
    };

    private PlanItem CopyOf(ItemKind kind) => _copiedItems.Single(i => i.Kind == kind);

    // ---------- Promote: instance plan -> template ----------

    [Test]
    public async Task PromoteToTemplateAsync_CarriesTheKindOfEveryRow()
    {
        // Arrange — the regression: every row arrived as a Drill, so a break became a drill
        // with no drill behind it and a Stations block became an unlabelled gap.
        var plan = BuildSourcePlan(PlanType.Instance);
        _planRepository.GetByIdWithDetailsAsync(SourcePlanId).Returns(plan);

        // Act
        await _sut.PromoteToTemplateAsync(SourcePlanId, new PromotePlanDto("Copy", null), UserId);

        // Assert
        _copiedItems.Select(i => i.Kind)
            .Should().Equal(ItemKind.Drill, ItemKind.Break, ItemKind.Stations);
    }

    [Test]
    public async Task PromoteToTemplateAsync_CarriesTitleNotesAndPlannedDuration()
    {
        // Arrange
        var plan = BuildSourcePlan(PlanType.Instance);
        _planRepository.GetByIdWithDetailsAsync(SourcePlanId).Returns(plan);

        // Act
        await _sut.PromoteToTemplateAsync(SourcePlanId, new PromotePlanDto("Copy", null), UserId);

        // Assert
        var water = CopyOf(ItemKind.Break);
        water.Title.Should().Be("Water");
        water.Notes.Should().Be("Refill bottles");
        water.DrillId.Should().BeNull();

        var stations = CopyOf(ItemKind.Stations);
        stations.Title.Should().Be("Stations");
        stations.Duration.Should().Be(20);
        stations.PlannedDuration.Should().Be(24);
    }

    [Test]
    public async Task PromoteToTemplateAsync_CopiesEveryGroupWithItsOwnRows()
    {
        // Arrange — two groups, not one: a copy that only ever reaches the first still passes
        // with a single group, which is how that shape survives review.
        var plan = BuildSourcePlan(PlanType.Instance);
        _planRepository.GetByIdWithDetailsAsync(SourcePlanId).Returns(plan);

        // Act
        await _sut.PromoteToTemplateAsync(SourcePlanId, new PromotePlanDto("Copy", null), UserId);

        // Assert
        var stations = CopyOf(ItemKind.Stations);
        stations.Stations.Select(s => s.Name).Should().ContainInOrder("Setters", "Hitters");

        var hitters = stations.Stations.Single(s => s.Name == "Hitters");
        hitters.Items.Should().HaveCount(2);
        hitters.Items.OrderBy(r => r.Order).First().DrillId.Should().Be(OtherDrillId);
    }

    [Test]
    public async Task PromoteToTemplateAsync_CopiesABreakInsideAGroupAsABreak()
    {
        // Arrange — one group takes water while the other keeps playing; the kind has to survive
        // the copy at group depth too, not only on the plan's own spine.
        var plan = BuildSourcePlan(PlanType.Instance);
        _planRepository.GetByIdWithDetailsAsync(SourcePlanId).Returns(plan);

        // Act
        await _sut.PromoteToTemplateAsync(SourcePlanId, new PromotePlanDto("Copy", null), UserId);

        // Assert
        var row = CopyOf(ItemKind.Stations).Stations
            .Single(s => s.Name == "Hitters").Items
            .Single(r => r.Kind == ItemKind.Break);
        row.Title.Should().Be("Water");
        row.DrillId.Should().BeNull();
        row.Duration.Should().Be(8);
        row.Notes.Should().Be("Short one");
    }

    [Test]
    public async Task PromoteToTemplateAsync_GivesTheCopyItsOwnIdsAndOwner()
    {
        // Arrange — the copy is a new plan's rows; sharing ids with the source would make an
        // edit to one show up in the other.
        var plan = BuildSourcePlan(PlanType.Instance);
        _planRepository.GetByIdWithDetailsAsync(SourcePlanId).Returns(plan);
        var sourceStationIds = plan.Items
            .SelectMany(i => i.Stations)
            .Select(s => s.Id)
            .Concat(plan.Items.SelectMany(i => i.Stations).SelectMany(s => s.Items).Select(r => r.Id))
            .ToList();

        // Act
        await _sut.PromoteToTemplateAsync(SourcePlanId, new PromotePlanDto("Copy", null), UserId);

        // Assert
        _copiedItems.Select(i => i.Id).Should().NotIntersectWith(plan.Items.Select(i => i.Id));
        _copiedItems.Select(i => i.TemplateId).Distinct().Should().NotContain(SourcePlanId);

        var copiedStationIds = _copiedItems
            .SelectMany(i => i.Stations)
            .Select(s => s.Id)
            .Concat(_copiedItems.SelectMany(i => i.Stations).SelectMany(s => s.Items).Select(r => r.Id));
        copiedStationIds.Should().NotIntersectWith(sourceStationIds);
    }

    [Test]
    public async Task PromoteToTemplateAsync_RepointsTheCopiedRowAtTheCopiedSection()
    {
        // Arrange
        var plan = BuildSourcePlan(PlanType.Instance);
        _planRepository.GetByIdWithDetailsAsync(SourcePlanId).Returns(plan);

        // Act
        await _sut.PromoteToTemplateAsync(SourcePlanId, new PromotePlanDto("Copy", null), UserId);

        // Assert
        CopyOf(ItemKind.Drill).SectionId.Should().Be(_copiedSections.Single().Id);
    }

    // ---------- Load: template -> event plan ----------
    //
    // The two directions share CopySectionsAndItemsAsync, so the field-by-field assertions above
    // hold for both. What this direction has of its own is the call that reaches the copy at all.

    [Test]
    public async Task CreateEventPlanAsync_FromATemplate_CopiesTheStationsBlockWithItsGroups()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var template = BuildSourcePlan(PlanType.Template);
        _planRepository.GetByIdWithDetailsAsync(SourcePlanId).Returns(template);
        _eventsGrpcClient.IsEventAdminAsync(eventId, UserId).Returns(true);

        // Act
        await _sut.CreateEventPlanAsync(eventId, new CreateEventPlanDto(null, null, SourcePlanId), UserId);

        // Assert
        var stations = CopyOf(ItemKind.Stations);
        stations.Stations.Should().HaveCount(2);
        stations.Stations.Sum(s => s.Items.Count).Should().Be(3);
        CopyOf(ItemKind.Break).Title.Should().Be("Water");
    }
}
