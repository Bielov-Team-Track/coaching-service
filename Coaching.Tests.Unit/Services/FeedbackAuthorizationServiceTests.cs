using Coaching.Application.DTOs.Feedback;
using Coaching.Application.Interfaces.Services;
using Coaching.Application.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shared.Enums;
using Shared.Exceptions;
using Shared.Testing.Base;

namespace Coaching.Tests.Unit.Services;

[TestFixture]
[Category("Unit")]
public class FeedbackAuthorizationServiceTests : UnitTestBase
{
    private IEventsGrpcClient _eventsClient = null!;
    private IClubsGrpcClient _clubsClient = null!;
    private ILogger<FeedbackAuthorizationService> _logger = null!;
    private FeedbackAuthorizationService _sut = null!;

    private static readonly Guid CoachId = Guid.NewGuid();
    private static readonly Guid PlayerId = Guid.NewGuid();
    private static readonly Guid ClubId = Guid.NewGuid();
    private static readonly Guid EventId = Guid.NewGuid();
    private static readonly Guid TeamId = Guid.NewGuid();
    private static readonly Guid GroupId = Guid.NewGuid();

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _eventsClient = Substitute.For<IEventsGrpcClient>();
        _clubsClient = Substitute.For<IClubsGrpcClient>();
        _logger = Substitute.For<ILogger<FeedbackAuthorizationService>>();
        _sut = new FeedbackAuthorizationService(_eventsClient, _clubsClient, _logger);
    }

    #region Event-linked feedback

    [Test]
    public async Task ValidateCreateAsync_EventLinkedClubEvent_CoachInClub_ReturnsClubId()
    {
        // Arrange
        var request = new CreateFeedbackDto { RecipientUserId = PlayerId, EventId = EventId };
        _eventsClient.GetEventContextAsync(EventId)
            .Returns(new EventContext("TrainingSession", "Club", ClubId));
        _eventsClient.IsEventParticipantAsync(EventId, PlayerId)
            .Returns((true, true));
        _clubsClient.CanGiveFeedbackInClubAsync(CoachId, ClubId)
            .Returns(true);

        // Act
        var resolvedClubId = await _sut.ValidateCreateAsync(request, CoachId);

        // Assert
        resolvedClubId.Should().Be(ClubId);
    }

    [Test]
    public async Task ValidateCreateAsync_EventLinkedClubEvent_NotCoach_ThrowsForbidden()
    {
        // Arrange
        var request = new CreateFeedbackDto { RecipientUserId = PlayerId, EventId = EventId };
        _eventsClient.GetEventContextAsync(EventId)
            .Returns(new EventContext("TrainingSession", "Club", ClubId));
        _eventsClient.IsEventParticipantAsync(EventId, PlayerId)
            .Returns((true, true));
        _clubsClient.CanGiveFeedbackInClubAsync(CoachId, ClubId)
            .Returns(false);

        // Act
        var act = () => _sut.ValidateCreateAsync(request, CoachId);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*coaches*club*");
    }

    [Test]
    public async Task ValidateCreateAsync_EventLinkedNonClub_EventAdmin_ReturnsNull()
    {
        // Arrange
        var request = new CreateFeedbackDto { RecipientUserId = PlayerId, EventId = EventId };
        _eventsClient.GetEventContextAsync(EventId)
            .Returns(new EventContext("Match", "None", null));
        _eventsClient.IsEventParticipantAsync(EventId, PlayerId)
            .Returns((true, true));
        _eventsClient.IsEventAdminAsync(EventId, CoachId)
            .Returns(true);

        // Act
        var resolvedClubId = await _sut.ValidateCreateAsync(request, CoachId);

        // Assert
        resolvedClubId.Should().BeNull();
    }

    [Test]
    public async Task ValidateCreateAsync_EventLinkedNonClub_NotAdmin_ThrowsForbidden()
    {
        // Arrange
        var request = new CreateFeedbackDto { RecipientUserId = PlayerId, EventId = EventId };
        _eventsClient.GetEventContextAsync(EventId)
            .Returns(new EventContext("Match", "None", null));
        _eventsClient.IsEventParticipantAsync(EventId, PlayerId)
            .Returns((true, true));
        _eventsClient.IsEventAdminAsync(EventId, CoachId)
            .Returns(false);

        // Act
        var act = () => _sut.ValidateCreateAsync(request, CoachId);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*organizers*admins*");
    }

    [Test]
    public async Task ValidateCreateAsync_EventLinkedGroupContext_FallsBackToEventAdmin()
    {
        // Arrange
        var request = new CreateFeedbackDto { RecipientUserId = PlayerId, EventId = EventId };
        var groupId = Guid.NewGuid();
        _eventsClient.GetEventContextAsync(EventId)
            .Returns(new EventContext("TrainingSession", "Group", groupId));
        _eventsClient.IsEventParticipantAsync(EventId, PlayerId)
            .Returns((true, true));
        _eventsClient.IsEventAdminAsync(EventId, CoachId)
            .Returns(true);

        // Act
        var resolvedClubId = await _sut.ValidateCreateAsync(request, CoachId);

        // Assert
        resolvedClubId.Should().BeNull();
    }

    [Test]
    public async Task ValidateCreateAsync_EventLinkedTeamContext_FallsBackToEventAdmin()
    {
        // Arrange
        var request = new CreateFeedbackDto { RecipientUserId = PlayerId, EventId = EventId };
        var teamId = Guid.NewGuid();
        _eventsClient.GetEventContextAsync(EventId)
            .Returns(new EventContext("Match", "Team", teamId));
        _eventsClient.IsEventParticipantAsync(EventId, PlayerId)
            .Returns((true, true));
        _eventsClient.IsEventAdminAsync(EventId, CoachId)
            .Returns(true);

        // Act
        var resolvedClubId = await _sut.ValidateCreateAsync(request, CoachId);

        // Assert
        resolvedClubId.Should().BeNull();
    }

    [Test]
    public async Task ValidateCreateAsync_WrongEventType_ThrowsForbidden()
    {
        // Arrange
        var request = new CreateFeedbackDto { RecipientUserId = PlayerId, EventId = EventId };
        _eventsClient.GetEventContextAsync(EventId)
            .Returns(new EventContext("CasualPlay", "Club", ClubId));

        // Act
        var act = () => _sut.ValidateCreateAsync(request, CoachId);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*CasualPlay*");
    }

    [Test]
    public async Task ValidateCreateAsync_RecipientNotParticipant_ThrowsForbidden()
    {
        // Arrange
        var request = new CreateFeedbackDto { RecipientUserId = PlayerId, EventId = EventId };
        _eventsClient.GetEventContextAsync(EventId)
            .Returns(new EventContext("TrainingSession", "Club", ClubId));
        _eventsClient.IsEventParticipantAsync(EventId, PlayerId)
            .Returns((false, true));

        // Act
        var act = () => _sut.ValidateCreateAsync(request, CoachId);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*recipient*not a participant*");
    }

    [Test]
    public async Task ValidateCreateAsync_EventNotFound_ThrowsForbidden()
    {
        // Arrange
        var request = new CreateFeedbackDto { RecipientUserId = PlayerId, EventId = EventId };
        _eventsClient.GetEventContextAsync(EventId).Returns((EventContext?)null);

        // Act
        var act = () => _sut.ValidateCreateAsync(request, CoachId);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*Event not found*");
    }

    [Test]
    public async Task ValidateCreateAsync_EventLinkedClubEvent_IgnoresRequestClubId()
    {
        // Arrange
        var differentClubId = Guid.NewGuid();
        var request = new CreateFeedbackDto
        {
            RecipientUserId = PlayerId,
            EventId = EventId,
            ClubId = differentClubId
        };
        _eventsClient.GetEventContextAsync(EventId)
            .Returns(new EventContext("TrainingSession", "Club", ClubId));
        _eventsClient.IsEventParticipantAsync(EventId, PlayerId)
            .Returns((true, true));
        _clubsClient.CanGiveFeedbackInClubAsync(CoachId, ClubId)
            .Returns(true);

        // Act
        var resolvedClubId = await _sut.ValidateCreateAsync(request, CoachId);

        // Assert
        resolvedClubId.Should().Be(ClubId);
        await _clubsClient.Received(1).CanGiveFeedbackInClubAsync(CoachId, ClubId);
        await _clubsClient.DidNotReceive().CanGiveFeedbackInClubAsync(CoachId, differentClubId);
    }


    [Test]
    public async Task ValidateCreateAsync_EventLinkedTeamEvent_TeamCoach_IsAdmittedWithoutBeingEventAdmin()
    {
        // Arrange
        var request = new CreateFeedbackDto { RecipientUserId = PlayerId, EventId = EventId };
        _eventsClient.GetEventContextAsync(EventId)
            .Returns(new EventContext("TrainingSession", "Team", TeamId));
        _eventsClient.IsEventParticipantAsync(EventId, PlayerId)
            .Returns((true, true));
        _eventsClient.IsEventAdminAsync(EventId, CoachId).Returns(false);
        _clubsClient.ResolveClubIdAsync(ContextType.Team, TeamId).Returns(ClubId);
        _clubsClient.CanGiveFeedbackInUnitAsync(CoachId, ContextType.Team, TeamId).Returns(true);

        // Act
        var act = () => _sut.ValidateCreateAsync(request, CoachId);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task ValidateCreateAsync_EventLinkedGroupEvent_GroupCoach_IsAdmittedWithoutBeingEventAdmin()
    {
        // Arrange
        var request = new CreateFeedbackDto { RecipientUserId = PlayerId, EventId = EventId };
        _eventsClient.GetEventContextAsync(EventId)
            .Returns(new EventContext("Evaluation", "Group", GroupId));
        _eventsClient.IsEventParticipantAsync(EventId, PlayerId)
            .Returns((true, true));
        _eventsClient.IsEventAdminAsync(EventId, CoachId).Returns(false);
        _clubsClient.ResolveClubIdAsync(ContextType.Group, GroupId).Returns(ClubId);
        _clubsClient.CanGiveFeedbackInUnitAsync(CoachId, ContextType.Group, GroupId).Returns(true);

        // Act
        var act = () => _sut.ValidateCreateAsync(request, CoachId);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task ValidateCreateAsync_EventLinkedTeamEvent_CoachOfTheOwningClub_IsAdmitted()
    {
        // Arrange
        var request = new CreateFeedbackDto { RecipientUserId = PlayerId, EventId = EventId };
        _eventsClient.GetEventContextAsync(EventId)
            .Returns(new EventContext("Match", "Team", TeamId));
        _eventsClient.IsEventParticipantAsync(EventId, PlayerId)
            .Returns((true, true));
        _eventsClient.IsEventAdminAsync(EventId, CoachId).Returns(false);
        _clubsClient.ResolveClubIdAsync(ContextType.Team, TeamId).Returns(ClubId);
        _clubsClient.CanGiveFeedbackInUnitAsync(CoachId, ContextType.Team, TeamId).Returns(false);
        _clubsClient.CanGiveFeedbackInClubAsync(CoachId, ClubId).Returns(true);

        // Act
        var act = () => _sut.ValidateCreateAsync(request, CoachId);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task ValidateCreateAsync_EventLinkedTeamEvent_NeitherUnitCoachNorEventAdmin_ThrowsForbidden()
    {
        // Arrange
        var request = new CreateFeedbackDto { RecipientUserId = PlayerId, EventId = EventId };
        _eventsClient.GetEventContextAsync(EventId)
            .Returns(new EventContext("Match", "Team", TeamId));
        _eventsClient.IsEventParticipantAsync(EventId, PlayerId)
            .Returns((true, true));
        _eventsClient.IsEventAdminAsync(EventId, CoachId).Returns(false);
        _clubsClient.ResolveClubIdAsync(ContextType.Team, TeamId).Returns(ClubId);
        _clubsClient.CanGiveFeedbackInUnitAsync(CoachId, ContextType.Team, TeamId).Returns(false);
        _clubsClient.CanGiveFeedbackInClubAsync(CoachId, ClubId).Returns(false);

        // Act
        var act = () => _sut.ValidateCreateAsync(request, CoachId);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    #endregion

    #region Standalone feedback

    [Test]
    public async Task ValidateCreateAsync_StandaloneWithClub_CoachAndMember_ReturnsClubId()
    {
        // Arrange
        var request = new CreateFeedbackDto { RecipientUserId = PlayerId, ClubId = ClubId };
        _clubsClient.CanGiveFeedbackInClubAsync(CoachId, ClubId).Returns(true);
        _clubsClient.IsUserClubMemberAsync(PlayerId, ClubId).Returns(true);

        // Act
        var resolvedClubId = await _sut.ValidateCreateAsync(request, CoachId);

        // Assert
        resolvedClubId.Should().Be(ClubId);
    }

    [Test]
    public async Task ValidateCreateAsync_StandaloneWithClub_NotCoach_ThrowsForbidden()
    {
        // Arrange
        var request = new CreateFeedbackDto { RecipientUserId = PlayerId, ClubId = ClubId };
        _clubsClient.CanGiveFeedbackInClubAsync(CoachId, ClubId).Returns(false);

        // Act
        var act = () => _sut.ValidateCreateAsync(request, CoachId);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*coaches*standalone*");
    }

    [Test]
    public async Task ValidateCreateAsync_StandaloneWithClub_RecipientNotMember_ThrowsForbidden()
    {
        // Arrange
        var request = new CreateFeedbackDto { RecipientUserId = PlayerId, ClubId = ClubId };
        _clubsClient.CanGiveFeedbackInClubAsync(CoachId, ClubId).Returns(true);
        _clubsClient.IsUserClubMemberAsync(PlayerId, ClubId).Returns(false);

        // Act
        var act = () => _sut.ValidateCreateAsync(request, CoachId);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*recipient*not a member*");
    }


    [Test]
    public async Task ValidateCreateAsync_StandaloneWithTeam_TeamCoachAndTeamMember_ReturnsOwningClubId()
    {
        // Arrange
        var request = new CreateFeedbackDto
        {
            RecipientUserId = PlayerId,
            ContextType = ContextType.Team,
            ContextId = TeamId
        };
        _clubsClient.ResolveClubIdAsync(ContextType.Team, TeamId).Returns(ClubId);
        _clubsClient.CanGiveFeedbackInUnitAsync(CoachId, ContextType.Team, TeamId).Returns(true);
        _clubsClient.IsUserUnitMemberAsync(PlayerId, ContextType.Team, TeamId).Returns(true);

        // Act
        var resolvedClubId = await _sut.ValidateCreateAsync(request, CoachId);

        // Assert
        resolvedClubId.Should().Be(ClubId);
    }

    [Test]
    public async Task ValidateCreateAsync_StandaloneWithGroup_GroupCoachAndGroupMember_ReturnsOwningClubId()
    {
        // Arrange
        var request = new CreateFeedbackDto
        {
            RecipientUserId = PlayerId,
            ContextType = ContextType.Group,
            ContextId = GroupId
        };
        _clubsClient.ResolveClubIdAsync(ContextType.Group, GroupId).Returns(ClubId);
        _clubsClient.CanGiveFeedbackInUnitAsync(CoachId, ContextType.Group, GroupId).Returns(true);
        _clubsClient.IsUserUnitMemberAsync(PlayerId, ContextType.Group, GroupId).Returns(true);

        // Act
        var resolvedClubId = await _sut.ValidateCreateAsync(request, CoachId);

        // Assert
        resolvedClubId.Should().Be(ClubId);
    }

    [Test]
    public async Task ValidateCreateAsync_StandaloneWithTeam_CoachOfTheOwningClub_IsAdmitted()
    {
        // Arrange
        var request = new CreateFeedbackDto
        {
            RecipientUserId = PlayerId,
            ContextType = ContextType.Team,
            ContextId = TeamId
        };
        _clubsClient.ResolveClubIdAsync(ContextType.Team, TeamId).Returns(ClubId);
        _clubsClient.CanGiveFeedbackInUnitAsync(CoachId, ContextType.Team, TeamId).Returns(false);
        _clubsClient.CanGiveFeedbackInClubAsync(CoachId, ClubId).Returns(true);
        _clubsClient.IsUserUnitMemberAsync(PlayerId, ContextType.Team, TeamId).Returns(true);

        // Act
        var resolvedClubId = await _sut.ValidateCreateAsync(request, CoachId);

        // Assert
        resolvedClubId.Should().Be(ClubId);
    }

    [Test]
    public async Task ValidateCreateAsync_StandaloneWithTeam_PlayerOfThatTeam_ThrowsForbidden()
    {
        // Arrange
        var request = new CreateFeedbackDto
        {
            RecipientUserId = PlayerId,
            ContextType = ContextType.Team,
            ContextId = TeamId
        };
        _clubsClient.ResolveClubIdAsync(ContextType.Team, TeamId).Returns(ClubId);
        _clubsClient.CanGiveFeedbackInUnitAsync(CoachId, ContextType.Team, TeamId).Returns(false);
        _clubsClient.CanGiveFeedbackInClubAsync(CoachId, ClubId).Returns(false);

        // Act
        var act = () => _sut.ValidateCreateAsync(request, CoachId);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*coaches*team or group*");
    }

    [Test]
    public async Task ValidateCreateAsync_StandaloneWithTeam_RecipientNotInThatTeam_ThrowsForbidden()
    {
        // Arrange
        var request = new CreateFeedbackDto
        {
            RecipientUserId = PlayerId,
            ContextType = ContextType.Team,
            ContextId = TeamId
        };
        _clubsClient.ResolveClubIdAsync(ContextType.Team, TeamId).Returns(ClubId);
        _clubsClient.CanGiveFeedbackInUnitAsync(CoachId, ContextType.Team, TeamId).Returns(true);
        _clubsClient.IsUserUnitMemberAsync(PlayerId, ContextType.Team, TeamId).Returns(false);

        // Act
        var act = () => _sut.ValidateCreateAsync(request, CoachId);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*recipient*not a member*team or group*");
    }

    [Test]
    public async Task ValidateCreateAsync_StandaloneWithTeam_UnknownTeam_ThrowsForbidden()
    {
        // Arrange
        var request = new CreateFeedbackDto
        {
            RecipientUserId = PlayerId,
            ContextType = ContextType.Team,
            ContextId = TeamId
        };
        _clubsClient.ResolveClubIdAsync(ContextType.Team, TeamId).Returns((Guid?)null);
        _clubsClient.CanGiveFeedbackInUnitAsync(CoachId, ContextType.Team, TeamId).Returns(false);

        // Act
        var act = () => _sut.ValidateCreateAsync(request, CoachId);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
        await _clubsClient.DidNotReceive().CanGiveFeedbackInClubAsync(CoachId, Arg.Any<Guid>());
    }

    [Test]
    public async Task ValidateCreateAsync_StandaloneWithTeam_IgnoresRequestClubId()
    {
        // Arrange
        var spoofedClubId = Guid.NewGuid();
        var request = new CreateFeedbackDto
        {
            RecipientUserId = PlayerId,
            ClubId = spoofedClubId,
            ContextType = ContextType.Team,
            ContextId = TeamId
        };
        _clubsClient.ResolveClubIdAsync(ContextType.Team, TeamId).Returns(ClubId);
        _clubsClient.CanGiveFeedbackInUnitAsync(CoachId, ContextType.Team, TeamId).Returns(true);
        _clubsClient.IsUserUnitMemberAsync(PlayerId, ContextType.Team, TeamId).Returns(true);

        // Act
        var resolvedClubId = await _sut.ValidateCreateAsync(request, CoachId);

        // Assert
        resolvedClubId.Should().Be(ClubId);
        await _clubsClient.DidNotReceive().CanGiveFeedbackInClubAsync(CoachId, spoofedClubId);
    }

    [Test]
    public async Task ValidateCreateAsync_StandaloneWithClubContextType_UsesTheClubRule()
    {
        // Arrange
        var request = new CreateFeedbackDto
        {
            RecipientUserId = PlayerId,
            ClubId = ClubId,
            ContextType = ContextType.Club,
            ContextId = ClubId
        };
        _clubsClient.CanGiveFeedbackInClubAsync(CoachId, ClubId).Returns(true);
        _clubsClient.IsUserClubMemberAsync(PlayerId, ClubId).Returns(true);

        // Act
        var resolvedClubId = await _sut.ValidateCreateAsync(request, CoachId);

        // Assert
        resolvedClubId.Should().Be(ClubId);
        await _clubsClient.DidNotReceive()
            .CanGiveFeedbackInUnitAsync(CoachId, Arg.Any<ContextType>(), Arg.Any<Guid>());
    }

    #endregion

    #region Edge cases

    [Test]
    public async Task ValidateCreateAsync_NoEventNoClub_ThrowsForbidden()
    {
        // Arrange
        var request = new CreateFeedbackDto { RecipientUserId = PlayerId };

        // Act
        var act = () => _sut.ValidateCreateAsync(request, CoachId);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*eventId or clubId*");
    }

    [Test]
    public async Task ValidateCreateAsync_SelfFeedback_ThrowsForbidden()
    {
        // Arrange
        var request = new CreateFeedbackDto { RecipientUserId = CoachId, ClubId = ClubId };

        // Act
        var act = () => _sut.ValidateCreateAsync(request, CoachId);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*yourself*");
    }

    #endregion

    #region CanCreateAsync (non-throwing)

    [Test]
    public async Task CanCreateAsync_Authorized_ReturnsTrue()
    {
        // Arrange
        var request = new CreateFeedbackDto { RecipientUserId = PlayerId, ClubId = ClubId };
        _clubsClient.CanGiveFeedbackInClubAsync(CoachId, ClubId).Returns(true);
        _clubsClient.IsUserClubMemberAsync(PlayerId, ClubId).Returns(true);

        // Act
        var canCreate = await _sut.CanCreateAsync(request, CoachId);

        // Assert
        canCreate.Should().BeTrue();
    }

    [Test]
    public async Task CanCreateAsync_Unauthorized_ReturnsFalse()
    {
        // Arrange
        var request = new CreateFeedbackDto { RecipientUserId = PlayerId, ClubId = ClubId };
        _clubsClient.CanGiveFeedbackInClubAsync(CoachId, ClubId).Returns(false);

        // Act
        var canCreate = await _sut.CanCreateAsync(request, CoachId);

        // Assert
        canCreate.Should().BeFalse();
    }

    #endregion
}
