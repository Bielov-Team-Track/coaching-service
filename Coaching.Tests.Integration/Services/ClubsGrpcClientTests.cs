using Coaching.Infrastructure.Services;
using FluentAssertions;
using Grpc.Core;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shared.Contracts.Grpc;

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

    private static async Task AssertCoachAccessAsync(string role, bool isMember, bool expected)
    {
        // Arrange
        var response = new CheckUserClubRolesResponse { IsMember = isMember };
        response.Roles.Add(role);

        var grpcClient = Substitute.For<ClubsInternalService.ClubsInternalServiceClient>();
        grpcClient.CheckUserClubRolesAsync(
                Arg.Any<CheckUserClubRolesRequest>(),
                Arg.Any<Metadata>(),
                Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(CompletedCall(response));

        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new ClubsGrpcClient(
            grpcClient,
            cache,
            Substitute.For<ILogger<ClubsGrpcClient>>());

        // Act
        var result = await sut.IsUserCoachInClubAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        result.Should().Be(expected);
    }

    private static AsyncUnaryCall<T> CompletedCall<T>(T response) => new(
        Task.FromResult(response),
        Task.FromResult(new Metadata()),
        () => Status.DefaultSuccess,
        () => new Metadata(),
        () => { });
}
