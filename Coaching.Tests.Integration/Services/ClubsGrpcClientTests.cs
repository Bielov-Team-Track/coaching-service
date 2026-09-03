using Coaching.Infrastructure.Services;
using FluentAssertions;
using Grpc.Core;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shared.Contracts.Grpc;
using Shared.Enums;

namespace Coaching.Tests.Integration.Services;

[TestFixture]
[Category("Unit")]
public class ClubsGrpcClientTests
{
    [TestCase("HeadCoach", true)]
    [TestCase("Admin", true)]
    [TestCase("Owner", true)]
    [TestCase("Member", false)]
    public async Task IsUserCoachInClubAsync_UsesClubsServiceHeadCoachOrAboveRoles(
        string role,
        bool expected)
    {
        await AssertCoachAccessAsync(role, isMember: true, expected);
    }

    [Test]
    public async Task IsUserCoachInClubAsync_InactiveMemberWithCoachRole_ReturnsFalse()
    {
        await AssertCoachAccessAsync("HeadCoach", isMember: false, expected: false);
    }

    [TestCase("Owner", true)]
    [TestCase("HeadCoach", true)]
    [TestCase("Coach", true)]
    [TestCase("Admin", true)]
    [TestCase("Treasurer", false)]
    [TestCase("WelfareOfficer", false)]
    [TestCase("Member", false)]
    public async Task CanGiveFeedbackInClubAsync_AdmitsOnlyClubRolesThatCoach(
        string role,
        bool expected)
    {
        // Arrange
        var sut = BuildSut(ClubRoles(isMember: true, role));

        // Act
        var result = await sut.CanGiveFeedbackInClubAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        result.Should().Be(expected);
    }

    [Test]
    public async Task CanGiveFeedbackInClubAsync_MemberAlsoHoldingCoach_ReturnsTrue()
    {
        // Arrange
        var sut = BuildSut(ClubRoles(isMember: true, "Member", "Coach"));

        // Act
        var result = await sut.CanGiveFeedbackInClubAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public async Task CanGiveFeedbackInClubAsync_InactiveMemberWithCoachRole_ReturnsFalse()
    {
        // Arrange
        var sut = BuildSut(ClubRoles(isMember: false, "Coach"));

        // Act
        var result = await sut.CanGiveFeedbackInClubAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    [TestCase(ContextType.Team, "Coach", true)]
    [TestCase(ContextType.Team, "AssistantCoach", true)]
    [TestCase(ContextType.Team, "Manager", false)]
    [TestCase(ContextType.Team, "Admin", false)]
    [TestCase(ContextType.Team, "Captain", false)]
    [TestCase(ContextType.Team, "Player", false)]
    [TestCase(ContextType.Group, "Coach", true)]
    [TestCase(ContextType.Group, "AssistantCoach", true)]
    [TestCase(ContextType.Group, "Admin", false)]
    [TestCase(ContextType.Group, "Helper", false)]
    [TestCase(ContextType.Group, "Member", false)]
    public async Task CanGiveFeedbackInUnitAsync_AdmitsOnlyUnitRolesThatCoach(
        ContextType contextType,
        string role,
        bool expected)
    {
        // Arrange
        var sut = BuildSut(UnitMembership(isMember: true, role));

        // Act
        var result = await sut.CanGiveFeedbackInUnitAsync(Guid.NewGuid(), contextType, Guid.NewGuid());

        // Assert
        result.Should().Be(expected);
    }

    [Test]
    public async Task CanGiveFeedbackInUnitAsync_PlayerAlsoHoldingAssistantCoach_ReturnsTrue()
    {
        // Arrange
        var sut = BuildSut(UnitMembership(isMember: true, "Player", "AssistantCoach"));

        // Act
        var result = await sut.CanGiveFeedbackInUnitAsync(Guid.NewGuid(), ContextType.Team, Guid.NewGuid());

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public async Task CanGiveFeedbackInUnitAsync_NonMemberCarryingNoRoles_ReturnsFalse()
    {
        // Arrange
        var sut = BuildSut(UnitMembership(isMember: false));

        // Act
        var result = await sut.CanGiveFeedbackInUnitAsync(Guid.NewGuid(), ContextType.Team, Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public async Task CanGiveFeedbackInUnitAsync_AsksClubsServiceForTheNamedContext()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var grpcClient = GrpcClientReturning(UnitMembership(isMember: true, "Coach"));
        var sut = BuildSut(grpcClient);

        // Act
        await sut.CanGiveFeedbackInUnitAsync(userId, ContextType.Team, teamId);

        // Assert — not awaited: the generated method answers AsyncUnaryCall, which is null
        // while the substitute is being queried rather than called.
        grpcClient.Received(1).GetMembershipAsync(
            Arg.Is<GetMembershipRequest>(r =>
                r.ContextType == "Team"
                && r.ContextId == teamId.ToString()
                && r.UserId == userId.ToString()),
            Arg.Any<Metadata>(),
            Arg.Any<DateTime?>(),
            Arg.Any<CancellationToken>());
    }

    [TestCase(true, true)]
    [TestCase(false, false)]
    public async Task IsUserUnitMemberAsync_FollowsClubsServiceMembership(bool isMember, bool expected)
    {
        // Arrange
        var sut = BuildSut(UnitMembership(isMember, "Player"));

        // Act
        var result = await sut.IsUserUnitMemberAsync(Guid.NewGuid(), ContextType.Team, Guid.NewGuid());

        // Assert
        result.Should().Be(expected);
    }

    [Test]
    public async Task ResolveClubIdAsync_UnitFound_ReturnsOwningClub()
    {
        // Arrange
        var clubId = Guid.NewGuid();
        var sut = BuildSut(new ResolveClubIdResponse { Found = true, ClubId = clubId.ToString() });

        // Act
        var result = await sut.ResolveClubIdAsync(ContextType.Group, Guid.NewGuid());

        // Assert
        result.Should().Be(clubId);
    }

    [Test]
    public async Task ResolveClubIdAsync_UnitNotFound_ReturnsNull()
    {
        // Arrange
        var sut = BuildSut(new ResolveClubIdResponse { Found = false });

        // Act
        var result = await sut.ResolveClubIdAsync(ContextType.Group, Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    private static async Task AssertCoachAccessAsync(string role, bool isMember, bool expected)
    {
        // Arrange
        var sut = BuildSut(ClubRoles(isMember, role));

        // Act
        var result = await sut.IsUserCoachInClubAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        result.Should().Be(expected);
    }

    private static CheckUserClubRolesResponse ClubRoles(bool isMember, params string[] roles)
    {
        var response = new CheckUserClubRolesResponse { IsMember = isMember };
        response.Roles.AddRange(roles);
        return response;
    }

    private static GetMembershipResponse UnitMembership(bool isMember, params string[] roles)
    {
        var response = new GetMembershipResponse { IsMember = isMember };
        response.Roles.AddRange(roles);
        return response;
    }

    private static ClubsGrpcClient BuildSut(CheckUserClubRolesResponse response) =>
        BuildSut(GrpcClientReturning(response));

    private static ClubsGrpcClient BuildSut(GetMembershipResponse response) =>
        BuildSut(GrpcClientReturning(response));

    private static ClubsGrpcClient BuildSut(ResolveClubIdResponse response) =>
        BuildSut(GrpcClientReturning(response));

    private static ClubsGrpcClient BuildSut(ClubsInternalService.ClubsInternalServiceClient grpcClient) =>
        new(grpcClient,
            new MemoryCache(new MemoryCacheOptions()),
            Substitute.For<ILogger<ClubsGrpcClient>>());

    private static ClubsInternalService.ClubsInternalServiceClient GrpcClientReturning(
        CheckUserClubRolesResponse response)
    {
        var grpcClient = Substitute.For<ClubsInternalService.ClubsInternalServiceClient>();
        grpcClient.CheckUserClubRolesAsync(
                Arg.Any<CheckUserClubRolesRequest>(),
                Arg.Any<Metadata>(),
                Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(CompletedCall(response));
        return grpcClient;
    }

    private static ClubsInternalService.ClubsInternalServiceClient GrpcClientReturning(
        GetMembershipResponse response)
    {
        var grpcClient = Substitute.For<ClubsInternalService.ClubsInternalServiceClient>();
        grpcClient.GetMembershipAsync(
                Arg.Any<GetMembershipRequest>(),
                Arg.Any<Metadata>(),
                Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(CompletedCall(response));
        return grpcClient;
    }

    private static ClubsInternalService.ClubsInternalServiceClient GrpcClientReturning(
        ResolveClubIdResponse response)
    {
        var grpcClient = Substitute.For<ClubsInternalService.ClubsInternalServiceClient>();
        grpcClient.ResolveClubIdAsync(
                Arg.Any<ResolveClubIdRequest>(),
                Arg.Any<Metadata>(),
                Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(CompletedCall(response));
        return grpcClient;
    }

    private static AsyncUnaryCall<T> CompletedCall<T>(T response) => new(
        Task.FromResult(response),
        Task.FromResult(new Metadata()),
        () => Status.DefaultSuccess,
        () => new Metadata(),
        () => { });
}
