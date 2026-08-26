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

namespace Coaching.Tests.Unit.Services;

/// <summary>
/// The plan pickers send the client's own filter vocabulary. Sorts it does not recognise fall
/// through to newest, and a skills filter it ignores returns plans that do not teach the skill —
/// both look like a working control that quietly does nothing.
/// </summary>
[TestFixture]
[Category("Unit")]
public class TrainingPlanFilterTests
{
    private ITrainingPlanRepository _planRepository = null!;
    private TrainingPlanService _sut = null!;

    private static readonly Guid UserId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _planRepository = Substitute.For<ITrainingPlanRepository>();

        var mapper = Substitute.For<IMapper>();
        mapper.Map<List<TrainingPlanDto>>(Arg.Any<IEnumerable<TrainingPlan>>())
            .Returns(call => call.Arg<IEnumerable<TrainingPlan>>()
                .Select(p => new TrainingPlanDto { Id = p.Id, Name = p.Name })
                .ToList());

        _sut = new TrainingPlanService(
            _planRepository,
            Substitute.For<IPlanSectionRepository>(),
            Substitute.For<IPlanItemRepository>(),
            Substitute.For<IPlanLikeRepository>(),
            Substitute.For<IPlanBookmarkRepository>(),
            Substitute.For<IPlanCommentRepository>(),
            Substitute.For<IDrillRepository>(),
            Substitute.For<IClubsGrpcClient>(),
            Substitute.For<IEventsGrpcClient>(),
            Substitute.For<IPublishEndpoint>(),
            mapper,
            Substitute.For<ILogger<TrainingPlanService>>());
    }

    private static TrainingPlan BuildPlan(string name, int duration = 60, params DrillSkill[] skills) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        CreatedByUserId = UserId,
        PlanType = PlanType.Template,
        Visibility = TemplateVisibility.Private,
        TotalDuration = duration,
        Items = skills.Length == 0
            ? new List<PlanItem>()
            : new List<PlanItem>
            {
                new()
                {
                    Duration = duration,
                    Drill = new Drill { Name = $"{name} drill", CreatedByUserId = UserId, Skills = skills }
                }
            }
    };

    private void StubPlans(params TrainingPlan[] plans) =>
        _planRepository.Query().Returns(plans.ToList().BuildMock());

    [Test]
    public async Task GetMyPlansAsync_SortByShortest_OrdersByAscendingDuration()
    {
        // Arrange
        StubPlans(BuildPlan("Long", 120), BuildPlan("Short", 30), BuildPlan("Middle", 60));

        // Act
        var result = await _sut.GetMyPlansAsync(UserId, new PlanFilterRequest { SortBy = "shortest" });

        // Assert
        result.Items.Select(p => p.Name).Should().Equal("Short", "Middle", "Long");
    }

    [Test]
    public async Task GetMyPlansAsync_SortByLongest_OrdersByDescendingDuration()
    {
        // Arrange
        StubPlans(BuildPlan("Short", 30), BuildPlan("Long", 120), BuildPlan("Middle", 60));

        // Act
        var result = await _sut.GetMyPlansAsync(UserId, new PlanFilterRequest { SortBy = "longest" });

        // Assert
        result.Items.Select(p => p.Name).Should().Equal("Long", "Middle", "Short");
    }

    [Test]
    public async Task GetMyPlansAsync_SkillsFilter_KeepsOnlyPlansTeachingOneOfThem()
    {
        // Arrange
        StubPlans(
            BuildPlan("Serving day", 60, DrillSkill.Serving),
            BuildPlan("Blocking day", 60, DrillSkill.Blocking),
            BuildPlan("Mixed day", 60, DrillSkill.Passing, DrillSkill.Serving));

        // Act
        var result = await _sut.GetMyPlansAsync(UserId, new PlanFilterRequest { Skills = ["Serving"] });

        // Assert
        result.Items.Select(p => p.Name).Should().BeEquivalentTo("Serving day", "Mixed day");
        result.TotalCount.Should().Be(2);
    }

    [Test]
    public async Task GetMyPlansAsync_UnknownSkillName_LeavesTheListAlone()
    {
        // Arrange
        StubPlans(BuildPlan("Serving day", 60, DrillSkill.Serving));

        // Act
        var result = await _sut.GetMyPlansAsync(UserId, new PlanFilterRequest { Skills = ["Juggling"] });

        // Assert
        result.Items.Should().HaveCount(1);
    }
}
