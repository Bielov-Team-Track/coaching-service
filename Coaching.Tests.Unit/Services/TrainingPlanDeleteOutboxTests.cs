using Coaching.Application.Interfaces.Repositories;
using Coaching.Application.Interfaces.Services;
using Coaching.Application.Services;
using Coaching.Domain.Enums;
using Coaching.Domain.Models.Templates;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using MockQueryable;
using Shared.DataAccess.Repositories.Interfaces;
using NSubstitute;
using Shared.Messaging.Contracts.Events.Coaching;
using Shared.Services.Analytics;

namespace Coaching.Tests.Unit.Services;

/// <summary>
/// Deleting an event's plan tells events-service to clear its TrainingPlanId and summary.
/// The bus outbox only ships what a SaveChanges commits, so the publish needs its own flush —
/// without one the message is written and never sent, and the plan's header outlives the plan.
/// </summary>
[TestFixture]
[Category("Unit")]
public class TrainingPlanDeleteOutboxTests
{
    private ITrainingPlanRepository _planRepository = null!;
    private IPublishEndpoint _publishEndpoint = null!;
    private TrainingPlanService _sut = null!;

    private static readonly Guid UserId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _planRepository = Substitute.For<ITrainingPlanRepository>();
        _publishEndpoint = Substitute.For<IPublishEndpoint>();

        var dialValues = Substitute.For<IRepository<PlanItemDialValue>>();
        dialValues.Query().Returns(new List<PlanItemDialValue>().BuildMock());

        _sut = new TrainingPlanService(
            _planRepository,
            Substitute.For<IPlanSectionRepository>(),
            Substitute.For<IPlanItemRepository>(),
            Substitute.For<IPlanLikeRepository>(),
            Substitute.For<IPlanBookmarkRepository>(),
            Substitute.For<IPlanCommentRepository>(),
            Substitute.For<IDrillRepository>(),
            dialValues,
            Substitute.For<IRepository<PlanStation>>(),
            Substitute.For<IRepository<PlanStationItem>>(),
            Substitute.For<IClubsGrpcClient>(),
            Substitute.For<IEventsGrpcClient>(),
            Substitute.For<IPlanCoachService>(),
            _publishEndpoint,
            Substitute.For<AutoMapper.IMapper>(),
            Substitute.For<ILogger<TrainingPlanService>>(),
            Substitute.For<IAnalyticsCapture>());
    }

    private TrainingPlan GivenPlan(PlanType type, Guid? eventId)
    {
        var plan = new TrainingPlan
        {
            Id = Guid.NewGuid(),
            Name = "Session",
            CreatedByUserId = UserId,
            PlanType = type,
            EventId = eventId,
        };
        _planRepository.GetByIdWithDetailsAsync(plan.Id).Returns(plan);
        return plan;
    }

    [Test]
    public async Task DeleteAsync_flushes_the_outbox_after_announcing_an_event_plans_removal()
    {
        var plan = GivenPlan(PlanType.Instance, Guid.NewGuid());

        await _sut.DeleteAsync(plan.Id, UserId);

        await _publishEndpoint.Received(1).Publish(
            Arg.Is<TrainingPlanUpdatedEvent>(e => e.Action == "Deleted" && e.TargetEventId == plan.EventId),
            Arg.Any<CancellationToken>());

        // One save commits the delete, the second ships the message the first one could not.
        await _planRepository.Received(2).SaveChangesAsync();
    }

    [Test]
    public async Task DeleteAsync_says_nothing_for_a_plan_no_event_is_using()
    {
        var plan = GivenPlan(PlanType.Template, null);

        await _sut.DeleteAsync(plan.Id, UserId);

        await _publishEndpoint.DidNotReceive().Publish(
            Arg.Any<TrainingPlanUpdatedEvent>(), Arg.Any<CancellationToken>());
        await _planRepository.Received(1).SaveChangesAsync();
    }
}
