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
using Shared.Services.Analytics;
using Shared.Testing.Base;

namespace Coaching.Tests.Unit.Services;

[TestFixture]
[Category("Unit")]
public class TrainingPlanValidationTests : UnitTestBase
{
    private ITrainingPlanRepository _planRepository = null!;
    private IDrillRepository _drillRepository = null!;
    private IMapper _mapper = null!;
    private TrainingPlanService _sut = null!;
    private Guid _userId;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _userId = Guid.NewGuid();
        _planRepository = Substitute.For<ITrainingPlanRepository>();
        _drillRepository = Substitute.For<IDrillRepository>();
        _mapper = Substitute.For<IMapper>();
        _mapper.Map<TrainingPlanDetailDto>(Arg.Any<TrainingPlan?>())
            .Returns(new TrainingPlanDetailDto { Name = "plan" });

        var dialValues = Substitute.For<IRepository<PlanItemDialValue>>();
        dialValues.Query().Returns(new List<PlanItemDialValue>().BuildMock());

        _sut = new TrainingPlanService(
            _planRepository,
            Substitute.For<IPlanSectionRepository>(),
            Substitute.For<IPlanItemRepository>(),
            Substitute.For<IPlanLikeRepository>(),
            Substitute.For<IPlanBookmarkRepository>(),
            Substitute.For<IPlanCommentRepository>(),
            _drillRepository,
            dialValues,
            Substitute.For<IRepository<PlanStation>>(),
            Substitute.For<IRepository<PlanStationItem>>(),
            Substitute.For<IClubsGrpcClient>(),
            Substitute.For<IEventsGrpcClient>(),
            Substitute.For<IPlanCoachService>(),
            Substitute.For<IPublishEndpoint>(),
            _mapper,
            Substitute.For<ILogger<TrainingPlanService>>(),
            Substitute.For<IAnalyticsCapture>());
    }

    private TrainingPlan ArrangeOwnedPlan()
    {
        var plan = new TrainingPlan
        {
            Id = Guid.NewGuid(),
            Name = "Plan",
            CreatedByUserId = _userId,
            PlanType = PlanType.Template
        };
        _planRepository.GetByIdWithDetailsAsync(plan.Id).Returns(plan);
        return plan;
    }

    private static UpdatePlanDto UpdateRequest(
        string? name = null,
        string? description = null,
        List<CreatePlanSectionDto>? sections = null,
        List<CreatePlanItemDto>? items = null)
        => new(name, description, null, null, null, sections, items);

    [Test]
    public async Task UpdateAsync_WhenDescriptionExceedsMaxLength_ThrowsValidationException()
    {
        // Arrange
        var plan = ArrangeOwnedPlan();
        var request = UpdateRequest(description: new string('x', 10_001));

        // Act
        var act = () => _sut.UpdateAsync(plan.Id, request, _userId);

        // Assert
        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.FieldErrors.Should().Contain(e => e.Field == "description" && e.Code == "INVALID_LENGTH");
    }

    [Test]
    public async Task UpdateAsync_WhenDescriptionWithinExpandedLimit_Saves()
    {
        // Arrange — 2500 chars exceeded the old 2000 column limit and must now be accepted
        var plan = ArrangeOwnedPlan();
        var request = UpdateRequest(description: new string('x', 2_500));

        // Act
        await _sut.UpdateAsync(plan.Id, request, _userId);

        // Assert
        await _planRepository.Received(1).SaveChangesAsync();
        plan.Description.Should().HaveLength(2_500);
    }

    [Test]
    public async Task CreateAsync_WhenNameExceedsMaxLength_ThrowsValidationException()
    {
        // Arrange
        var request = new CreatePlanDto(new string('x', 201), null, null);

        // Act
        var act = () => _sut.CreateAsync(request, _userId);

        // Assert
        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.FieldErrors.Should().Contain(e => e.Field == "name" && e.Code == "INVALID_LENGTH");
    }

    [Test]
    public async Task CreateAsync_WhenSectionNameAndItemNotesExceedMaxLength_ReportsBothFieldErrors()
    {
        // Arrange
        _drillRepository.GetByIdAsync(Arg.Any<Guid>()).Returns(new Drill { Name = "drill" });
        var request = new CreatePlanDto(
            "Plan",
            null,
            null,
            Sections: [new CreatePlanSectionDto(new string('x', 101), 1)],
            Items: [new CreatePlanItemDto(Guid.NewGuid(), null, 10, new string('x', PlanItem.NotesMaxLength + 1))]);

        // Act
        var act = () => _sut.CreateAsync(request, _userId);

        // Assert
        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.FieldErrors.Should().Contain(e => e.Field == "sections[0].name" && e.Code == "INVALID_LENGTH");
        ex.Which.FieldErrors.Should().Contain(e => e.Field == "items[0].notes" && e.Code == "INVALID_LENGTH");
    }

    [Test]
    public async Task CreateEventPlanAsync_WhenDescriptionExceedsMaxLength_ThrowsValidationException()
    {
        // Arrange
        var request = new CreateEventPlanDto("Plan", new string('x', 10_001), null);

        // Act
        var act = () => _sut.CreateEventPlanAsync(Guid.NewGuid(), request, _userId);

        // Assert
        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.FieldErrors.Should().Contain(e => e.Field == "description" && e.Code == "INVALID_LENGTH");
    }

    [Test]
    public async Task PromoteToTemplateAsync_WhenNameExceedsMaxLength_ThrowsValidationException()
    {
        // Arrange
        var request = new PromotePlanDto(new string('x', 201), null);

        // Act
        var act = () => _sut.PromoteToTemplateAsync(Guid.NewGuid(), request, _userId);

        // Assert
        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.FieldErrors.Should().Contain(e => e.Field == "name" && e.Code == "INVALID_LENGTH");
    }
}
