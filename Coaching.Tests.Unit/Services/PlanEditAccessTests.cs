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
using NSubstitute;
using Shared.Exceptions;
using Shared.Testing.Base;

namespace Coaching.Tests.Unit.Services;

/// <summary>
/// Who may change an event's plan. The owner always may, and so does the event's lead coach:
/// the person running the session has to be able to shape it, and they are often not whoever
/// first created the plan. A template has no event behind it, so it stays with its owner.
/// </summary>
[TestFixture]
[Category("Unit")]
public class PlanEditAccessTests : UnitTestBase
{
    private ITrainingPlanRepository _planRepository = null!;
    private IEventsGrpcClient _eventsGrpcClient = null!;
    private readonly List<TrainingPlan> _addedPlans = [];
    private TrainingPlanService _sut = null!;

    private static readonly Guid PlanId = Guid.NewGuid();
    private static readonly Guid EventId = Guid.NewGuid();
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid LeadCoachId = Guid.NewGuid();
    private static readonly Guid StrangerId = Guid.NewGuid();

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _addedPlans.Clear();

        _planRepository = Substitute.For<ITrainingPlanRepository>();
        _eventsGrpcClient = Substitute.For<IEventsGrpcClient>();
        _planRepository.When(r => r.Add(Arg.Any<TrainingPlan>())).Do(c => _addedPlans.Add(c.Arg<TrainingPlan>()));

        _eventsGrpcClient.IsEventAdminAsync(EventId, LeadCoachId).Returns(true);

        var mapper = Substitute.For<IMapper>();
        mapper.Map<TrainingPlanDetailDto>(Arg.Any<TrainingPlan?>()).Returns(new TrainingPlanDetailDto { Name = "plan" });

        _sut = new TrainingPlanService(
            _planRepository,
            Substitute.For<IPlanSectionRepository>(),
            Substitute.For<IPlanItemRepository>(),
            Substitute.For<IPlanLikeRepository>(),
            Substitute.For<IPlanBookmarkRepository>(),
            Substitute.For<IPlanCommentRepository>(),
            Substitute.For<IDrillRepository>(),
            Substitute.For<IClubsGrpcClient>(),
            _eventsGrpcClient,
            Substitute.For<IPlanCoachService>(),
            Substitute.For<IPublishEndpoint>(),
            mapper,
            Substitute.For<ILogger<TrainingPlanService>>());
    }

    private static TrainingPlan EventPlan() => new()
    {
        Id = PlanId,
        Name = "Friday practice",
        CreatedByUserId = OwnerId,
        PlanType = PlanType.Instance,
        EventId = EventId
    };

    private static TrainingPlan TemplatePlan() => new()
    {
        Id = PlanId,
        Name = "Reusable warm-up",
        CreatedByUserId = OwnerId,
        PlanType = PlanType.Template
    };

    private void StubPlan(TrainingPlan plan) =>
        _planRepository.GetByIdWithDetailsAsync(Arg.Any<Guid>())
            .Returns(ci => ci.Arg<Guid>() == PlanId ? plan : null);

    [Test]
    public async Task DeleteAsync_WhenTheCallerOwnsThePlan_Deletes()
    {
        // Arrange
        var plan = EventPlan();
        StubPlan(plan);

        // Act
        await _sut.DeleteAsync(PlanId, OwnerId);

        // Assert
        _planRepository.Received(1).Delete(plan);
    }

    [Test]
    public async Task DeleteAsync_WhenTheCallerIsTheEventLeadCoach_Deletes()
    {
        // Arrange — the widening this guards: an event admin who did not create the plan
        var plan = EventPlan();
        StubPlan(plan);

        // Act
        await _sut.DeleteAsync(PlanId, LeadCoachId);

        // Assert
        _planRepository.Received(1).Delete(plan);
    }

    [Test]
    public async Task DeleteAsync_WhenTheCallerIsAStranger_Throws()
    {
        // Arrange — not the owner, and not an admin of the event either
        StubPlan(EventPlan());

        // Act
        var act = () => _sut.DeleteAsync(PlanId, StrangerId);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
        _planRepository.DidNotReceive().Delete(Arg.Any<TrainingPlan>());
    }

    [Test]
    public async Task DeleteAsync_OnATemplate_StaysWithItsOwner()
    {
        // Arrange — a template has no event, so there is no lead coach to widen to. The event
        // admin of an unrelated event must not reach it.
        StubPlan(TemplatePlan());

        // Act
        var act = () => _sut.DeleteAsync(PlanId, LeadCoachId);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Test]
    public async Task PromoteToTemplateAsync_DoesNotCarryTheEventsCoachesOntoTheTemplate()
    {
        // Arrange — the plan is staffed; the template made from it is a shape for a future
        // event, which will be staffed by whoever is at that one.
        var plan = EventPlan();
        plan.Coaches.Add(new PlanCoach { PlanId = PlanId, UserId = LeadCoachId });
        plan.Coaches.Add(new PlanCoach { PlanId = PlanId, UserId = OwnerId });
        StubPlan(plan);

        // Act
        await _sut.PromoteToTemplateAsync(PlanId, new PromotePlanDto(null, null), OwnerId);

        // Assert
        _addedPlans.Should().ContainSingle().Which.Coaches.Should().BeEmpty();
    }
}
