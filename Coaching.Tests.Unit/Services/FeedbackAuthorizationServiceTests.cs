using Coaching.Application.DTOs.Feedback;
using Coaching.Application.Interfaces.Services;
using Coaching.Application.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
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
        _clubsClient.IsUserCoachInClubAsync(CoachId, ClubId)
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
        _clubsClient.IsUserCoachInClubAsync(CoachId, ClubId)
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
        _clubsClient.IsUserCoachInClubAsync(CoachId, ClubId)
            .Returns(true);

        // Act
        var resolvedClubId = await _sut.ValidateCreateAsync(request, CoachId);

        // Assert
        resolvedClubId.Should().Be(ClubId);
        await _clubsClient.Received(1).IsUserCoachInClubAsync(CoachId, ClubId);
        await _clubsClient.DidNotReceive().IsUserCoachInClubAsync(CoachId, differentClubId);
    }

    #endregion

    #region Standalone feedback

    [Test]
    public async Task ValidateCreateAsync_StandaloneWithClub_CoachAndMember_ReturnsClubId()
    {
        // Arrange
        var request = new CreateFeedbackDto { RecipientUserId = PlayerId, ClubId = ClubId };
        _clubsClient.IsUserCoachInClubAsync(CoachId, ClubId).Returns(true);
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
        _clubsClient.IsUserCoachInClubAsync(CoachId, ClubId).Returns(false);

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
        _clubsClient.IsUserCoachInClubAsync(CoachId, ClubId).Returns(true);
        _clubsClient.IsUserClubMemberAsync(PlayerId, ClubId).Returns(false);

        // Act
        var act = () => _sut.ValidateCreateAsync(request, CoachId);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*recipient*not a member*");
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
        _clubsClient.IsUserCoachInClubAsync(CoachId, ClubId).Returns(true);
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
        _clubsClient.IsUserCoachInClubAsync(CoachId, ClubId).Returns(false);

        // Act
        var canCreate = await _sut.CanCreateAsync(request, CoachId);

        // Assert
        canCreate.Should().BeFalse();
    }

    #endregion
}
