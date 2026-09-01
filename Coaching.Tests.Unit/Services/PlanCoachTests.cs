using Coaching.Application.DTOs.Templates;
using Coaching.Application.Interfaces.Repositories;
using Coaching.Application.Interfaces.Services;
using Coaching.Application.Services;
using Coaching.Domain.Enums;
using Coaching.Domain.Models.Templates;
using FluentAssertions;
using MockQueryable;
using NSubstitute;
using Shared.DataAccess.Repositories.Interfaces;
using Shared.Exceptions;
using Shared.Models;
using Shared.Testing.Base;

namespace Coaching.Tests.Unit.Services;

/// <summary>
/// Assigning coaches to an event's practice. The lead coach of the event hands out the work:
/// coaches go on the plan, or on one station, and only ever people who are actually at the
/// event. A template has no coaches — there is no event yet to staff.
/// </summary>
[TestFixture]
[Category("Unit")]
public class PlanCoachTests : UnitTestBase
{
    private ITrainingPlanRepository _planRepository = null!;
    private IRepository<PlanCoach> _planCoachRepository = null!;
    private IRepository<PlanStation> _stationRepository = null!;
    private IRepository<PlanStationCoach> _stationCoachRepository = null!;
    private IRepository<UserProfile> _userProfileRepository = null!;
    private IEventsGrpcClient _eventsGrpcClient = null!;
    private PlanCoachService _sut = null!;

    private readonly List<PlanCoach> _addedCoaches = [];
    private readonly List<PlanCoach> _deletedCoaches = [];
    private readonly List<PlanStationCoach> _addedStationCoaches = [];
    private readonly List<PlanStationCoach> _deletedStationCoaches = [];

    private static readonly Guid PlanId = Guid.NewGuid();
    private static readonly Guid EventId = Guid.NewGuid();
    private static readonly Guid StationId = Guid.NewGuid();
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid LeadCoachId = Guid.NewGuid();
    private static readonly Guid StrangerId = Guid.NewGuid();
    private static readonly Guid CoachAId = Guid.NewGuid();
    private static readonly Guid CoachBId = Guid.NewGuid();
    private static readonly Guid CoachCId = Guid.NewGuid();

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _addedCoaches.Clear();
        _deletedCoaches.Clear();
        _addedStationCoaches.Clear();
        _deletedStationCoaches.Clear();

        _planRepository = Substitute.For<ITrainingPlanRepository>();
        _planCoachRepository = Substitute.For<IRepository<PlanCoach>>();
        _stationRepository = Substitute.For<IRepository<PlanStation>>();
        _stationCoachRepository = Substitute.For<IRepository<PlanStationCoach>>();
        _userProfileRepository = Substitute.For<IRepository<UserProfile>>();
        _eventsGrpcClient = Substitute.For<IEventsGrpcClient>();

        _planCoachRepository.When(r => r.Add(Arg.Any<PlanCoach>())).Do(c => _addedCoaches.Add(c.Arg<PlanCoach>()));
        _planCoachRepository.When(r => r.Delete(Arg.Any<PlanCoach>())).Do(c => _deletedCoaches.Add(c.Arg<PlanCoach>()));
        _stationCoachRepository.When(r => r.Add(Arg.Any<PlanStationCoach>())).Do(c => _addedStationCoaches.Add(c.Arg<PlanStationCoach>()));
        _stationCoachRepository.When(r => r.Delete(Arg.Any<PlanStationCoach>())).Do(c => _deletedStationCoaches.Add(c.Arg<PlanStationCoach>()));

        StubPlan(EventPlan());
        StubExistingPlanCoaches();
        StubExistingStationCoaches();
        StubStation(onPlanId: PlanId);
        StubProfiles();

        // Everyone in this fixture is on the event unless a test says otherwise.
        _eventsGrpcClient.IsEventParticipantAsync(EventId, Arg.Any<Guid>()).Returns((true, true));
        _eventsGrpcClient.IsEventAdminAsync(EventId, LeadCoachId).Returns(true);

        _sut = new PlanCoachService(
            _planRepository,
            _planCoachRepository,
            _stationRepository,
            _stationCoachRepository,
            _userProfileRepository,
            _eventsGrpcClient);
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
        _planRepository.Query().Returns(new List<TrainingPlan> { plan }.BuildMock());

    private void StubExistingPlanCoaches(params Guid[] userIds) =>
        _planCoachRepository.Query().Returns(userIds
            .Select(id => new PlanCoach { PlanId = PlanId, UserId = id })
            .ToList()
            .BuildMock());

    private void StubExistingStationCoaches(params Guid[] userIds) =>
        _stationCoachRepository.Query().Returns(userIds
            .Select(id => new PlanStationCoach { StationId = StationId, UserId = id })
            .ToList()
            .BuildMock());

    private void StubStation(Guid onPlanId) =>
        _stationRepository.Query().Returns(new List<PlanStation>
        {
            new()
            {
                Id = StationId,
                Name = "Setters",
                Item = new PlanItem { TemplateId = onPlanId, Kind = ItemKind.Stations }
            }
        }.BuildMock());

    private void StubProfiles(params UserProfile[] profiles) =>
        _userProfileRepository.QueryNoTracking().Returns(profiles.ToList().BuildMock());

    private static AssignCoachesDto Assign(params Guid[] userIds) => new(userIds.ToList());

    [Test]
    public async Task ReplacePlanCoachesAsync_ReplacesTheSetRatherThanAddingToIt()
    {
        // Arrange — two already on, and the new set keeps one of them. A single-coach case
        // would pass even if this only ever appended, which is how that shape survives review.
        StubExistingPlanCoaches(CoachAId, CoachBId);

        // Act
        await _sut.ReplacePlanCoachesAsync(PlanId, Assign(CoachBId, CoachCId), OwnerId);

        // Assert
        _deletedCoaches.Select(c => c.UserId).Should().BeEquivalentTo([CoachAId]);
        _addedCoaches.Select(c => c.UserId).Should().BeEquivalentTo([CoachCId]);
    }

    [Test]
    public async Task ReplacePlanCoachesAsync_WithAnEmptyList_ClearsTheSet()
    {
        // Arrange
        StubExistingPlanCoaches(CoachAId, CoachBId);

        // Act
        var result = await _sut.ReplacePlanCoachesAsync(PlanId, new AssignCoachesDto([]), OwnerId);

        // Assert
        _deletedCoaches.Select(c => c.UserId).Should().BeEquivalentTo([CoachAId, CoachBId]);
        _addedCoaches.Should().BeEmpty();
        result.Should().BeEmpty();
    }

    [Test]
    public async Task ReplacePlanCoachesAsync_NamesTheSameCoachTwice_AssignsThemOnce()
    {
        // Act
        await _sut.ReplacePlanCoachesAsync(PlanId, Assign(CoachAId, CoachAId), OwnerId);

        // Assert
        _addedCoaches.Should().ContainSingle().Which.UserId.Should().Be(CoachAId);
    }

    [Test]
    public async Task ReplacePlanCoachesAsync_SavesOnce()
    {
        // Arrange
        StubExistingPlanCoaches(CoachAId);

        // Act
        await _sut.ReplacePlanCoachesAsync(PlanId, Assign(CoachBId), OwnerId);

        // Assert — the removal and the addition land together or not at all
        await _planCoachRepository.Received(1).SaveChangesAsync();
    }

    [Test]
    public async Task ReplacePlanCoachesAsync_WhenThePlanIsATemplate_Throws()
    {
        // Arrange — a template is a shape to reuse; who runs it is not known yet
        StubPlan(TemplatePlan());

        // Act
        var act = () => _sut.ReplacePlanCoachesAsync(PlanId, Assign(CoachAId), OwnerId);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Test]
    public async Task ReplacePlanCoachesAsync_WhenThePlanDoesNotExist_Throws()
    {
        // Arrange
        _planRepository.Query().Returns(new List<TrainingPlan>().BuildMock());

        // Act
        var act = () => _sut.ReplacePlanCoachesAsync(PlanId, Assign(CoachAId), OwnerId);

        // Assert
        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Test]
    public async Task ReplacePlanCoachesAsync_WhenTheCallerIsTheEventLeadCoach_Assigns()
    {
        // Act — the lead coach of the event distributes the work, not just whoever made the plan
        await _sut.ReplacePlanCoachesAsync(PlanId, Assign(CoachAId), LeadCoachId);

        // Assert
        _addedCoaches.Select(c => c.UserId).Should().BeEquivalentTo([CoachAId]);
    }

    [Test]
    public async Task ReplacePlanCoachesAsync_WhenTheCallerIsNeitherOwnerNorEventAdmin_Throws()
    {
        // Act
        var act = () => _sut.ReplacePlanCoachesAsync(PlanId, Assign(CoachAId), StrangerId);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Test]
    public async Task ReplacePlanCoachesAsync_WhenACoachIsNotOnTheEvent_ThrowsAndAssignsNobody()
    {
        // Arrange — CoachB never joined the event
        _eventsGrpcClient.IsEventParticipantAsync(EventId, CoachBId).Returns((false, true));

        // Act
        var act = () => _sut.ReplacePlanCoachesAsync(PlanId, Assign(CoachAId, CoachBId), OwnerId);

        // Assert — the whole assignment is refused, not half-applied
        (await act.Should().ThrowAsync<BadRequestException>())
            .Which.Message.Should().Contain(CoachBId.ToString());
        _addedCoaches.Should().BeEmpty();
        await _planCoachRepository.DidNotReceive().SaveChangesAsync();
    }

    [Test]
    public async Task ReplacePlanCoachesAsync_WhenTheEventIsGone_Throws()
    {
        // Arrange
        _eventsGrpcClient.IsEventParticipantAsync(EventId, CoachAId).Returns((false, false));

        // Act
        var act = () => _sut.ReplacePlanCoachesAsync(PlanId, Assign(CoachAId), OwnerId);

        // Assert
        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Test]
    public async Task ReplacePlanCoachesAsync_ResolvesNamesFromTheProfileReplica()
    {
        // Arrange
        StubProfiles(new UserProfile
        {
            Id = CoachAId,
            Name = "Nuria",
            Surname = "Roca",
            ImageUrl = "https://cdn/nuria.jpg",
            ImageThumbHash = "hash-nuria"
        });

        // Act
        var result = await _sut.ReplacePlanCoachesAsync(PlanId, Assign(CoachAId), OwnerId);

        // Assert
        var coach = result.Should().ContainSingle().Subject;
        coach.UserId.Should().Be(CoachAId);
        coach.FirstName.Should().Be("Nuria");
        coach.LastName.Should().Be("Roca");
        coach.AvatarUrl.Should().Be("https://cdn/nuria.jpg");
        coach.ImageThumbHash.Should().Be("hash-nuria");
    }

    [Test]
    public async Task ReplacePlanCoachesAsync_WhenAProfileHasNotReplicatedYet_StillReturnsTheCoach()
    {
        // Arrange — assignment is checked against the event roster, not the local replica, so a
        // coach whose profile has not arrived yet is assigned and simply comes back nameless.
        StubProfiles();

        // Act
        var result = await _sut.ReplacePlanCoachesAsync(PlanId, Assign(CoachAId), OwnerId);

        // Assert
        var coach = result.Should().ContainSingle().Subject;
        coach.UserId.Should().Be(CoachAId);
        coach.FirstName.Should().BeNull();
    }

    [Test]
    public async Task ResolveNamesAsync_LooksProfilesUpOnceForTheWholeBatch()
    {
        // Arrange — the plan's coaches plus every station's arrive together
        StubProfiles(
            new UserProfile { Id = CoachAId, Name = "Nuria" },
            new UserProfile { Id = CoachBId, Name = "Iker" });
        var coaches = new List<PlanCoachDto>
        {
            new() { UserId = CoachAId },
            new() { UserId = CoachBId },
            new() { UserId = CoachAId }
        };

        // Act
        await _sut.ResolveNamesAsync(coaches);

        // Assert
        coaches.Select(c => c.FirstName).Should().Equal("Nuria", "Iker", "Nuria");
        _userProfileRepository.Received(1).QueryNoTracking();
    }

    [Test]
    public async Task ReplaceStationCoachesAsync_AssignsToThatStation()
    {
        // Act
        await _sut.ReplaceStationCoachesAsync(PlanId, StationId, Assign(CoachAId), OwnerId);

        // Assert
        _addedStationCoaches.Should().ContainSingle()
            .Which.Should().Match<PlanStationCoach>(c => c.StationId == StationId && c.UserId == CoachAId);
    }

    [Test]
    public async Task ReplaceStationCoachesAsync_ReplacesThatStationsSet()
    {
        // Arrange
        StubExistingStationCoaches(CoachAId, CoachBId);

        // Act
        await _sut.ReplaceStationCoachesAsync(PlanId, StationId, Assign(CoachBId, CoachCId), OwnerId);

        // Assert
        _deletedStationCoaches.Select(c => c.UserId).Should().BeEquivalentTo([CoachAId]);
        _addedStationCoaches.Select(c => c.UserId).Should().BeEquivalentTo([CoachCId]);
    }

    [Test]
    public async Task ReplaceStationCoachesAsync_WhenTheStationBelongsToAnotherPlan_Throws()
    {
        // Arrange — rights on your own plan must not reach into someone else's station
        StubStation(onPlanId: Guid.NewGuid());

        // Act
        var act = () => _sut.ReplaceStationCoachesAsync(PlanId, StationId, Assign(CoachAId), OwnerId);

        // Assert
        await act.Should().ThrowAsync<EntityNotFoundException>();
        _addedStationCoaches.Should().BeEmpty();
    }

    [Test]
    public async Task ReplaceStationCoachesAsync_WhenThePlanIsATemplate_Throws()
    {
        // Arrange
        StubPlan(TemplatePlan());

        // Act
        var act = () => _sut.ReplaceStationCoachesAsync(PlanId, StationId, Assign(CoachAId), OwnerId);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Test]
    public async Task ReplaceStationCoachesAsync_WhenTheCallerIsNeitherOwnerNorEventAdmin_Throws()
    {
        // Act
        var act = () => _sut.ReplaceStationCoachesAsync(PlanId, StationId, Assign(CoachAId), StrangerId);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
