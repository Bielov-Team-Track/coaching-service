using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Coaching.Tests.Integration.Fixtures;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using NSubstitute.ClearExtensions;
using Shared.Enums;

namespace Coaching.Tests.Integration.Controllers;

/// <summary>
/// The question the clients ask before they draw a "Give feedback" button. A team's coach holds
/// their coaching role on the team, not on the club row, so the endpoint has to be askable about
/// a team or a group — asking about the club alone is what hid the button (SPI-5906).
/// </summary>
[TestFixture]
[Category("Integration")]
public class FeedbackCanCreateControllerTests
{
    private CoachingApiFactory _factory = null!;
    private HttpClient _client = null!;

    private record CanCreateResponse(bool CanCreate);

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _factory = new CoachingApiFactory();
        await _factory.InitializeAsync();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown() => await _factory.DisposeAsync();

    [TearDown]
    public void TearDown()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        _factory.ClubsGrpcClient.ClearSubstitute();
    }

    [TestCase(ContextType.Team)]
    [TestCase(ContextType.Group)]
    public async Task CanCreate_CoachOfTheUnitAskingAboutItsPlayer_AnswersYes(ContextType contextType)
    {
        // Arrange
        var coachId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        _factory.ClubsGrpcClient.CanGiveFeedbackInUnitAsync(coachId, contextType, unitId).Returns(true);
        _factory.ClubsGrpcClient.IsUserUnitMemberAsync(playerId, contextType, unitId).Returns(true);
        _factory.ClubsGrpcClient.ResolveClubIdAsync(contextType, unitId).Returns(Guid.NewGuid());
        SetAuth(coachId);

        // Act
        var response = await GetCanCreateAsync(
            $"recipientUserId={playerId}&contextType={contextType}&contextId={unitId}");

        // Assert
        response!.CanCreate.Should().BeTrue();
    }

    [Test]
    public async Task CanCreate_PlayerAskingAboutATeammate_AnswersNo()
    {
        // Arrange
        var playerId = Guid.NewGuid();
        var teammateId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        _factory.ClubsGrpcClient.IsUserUnitMemberAsync(teammateId, ContextType.Team, teamId).Returns(true);
        _factory.ClubsGrpcClient.ResolveClubIdAsync(ContextType.Team, teamId).Returns(Guid.NewGuid());
        SetAuth(playerId);

        // Act
        var response = await GetCanCreateAsync(
            $"recipientUserId={teammateId}&contextType=Team&contextId={teamId}");

        // Assert
        response!.CanCreate.Should().BeFalse();
    }

    [Test]
    public async Task CanCreate_UnitNamedButRecipientPlaysElsewhere_AnswersNo()
    {
        // Arrange
        var coachId = Guid.NewGuid();
        var strangerId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        _factory.ClubsGrpcClient.CanGiveFeedbackInUnitAsync(coachId, ContextType.Team, teamId).Returns(true);
        _factory.ClubsGrpcClient.ResolveClubIdAsync(ContextType.Team, teamId).Returns(Guid.NewGuid());
        SetAuth(coachId);

        // Act
        var response = await GetCanCreateAsync(
            $"recipientUserId={strangerId}&contextType=Team&contextId={teamId}");

        // Assert
        response!.CanCreate.Should().BeFalse();
    }

    [Test]
    public async Task CanCreate_ClubAskedWithoutAUnit_StillAnswersFromTheClubRoles()
    {
        // Arrange
        var coachId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        var clubId = Guid.NewGuid();
        _factory.ClubsGrpcClient.CanGiveFeedbackInClubAsync(coachId, clubId).Returns(true);
        _factory.ClubsGrpcClient.IsUserClubMemberAsync(playerId, clubId).Returns(true);
        SetAuth(coachId);

        // Act
        var response = await GetCanCreateAsync($"recipientUserId={playerId}&clubId={clubId}");

        // Assert
        response!.CanCreate.Should().BeTrue();
    }

    private async Task<CanCreateResponse?> GetCanCreateAsync(string query)
    {
        var response = await _client.GetAsync($"/v1/feedback/can-create?{query}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CanCreateResponse>();
    }

    private void SetAuth(Guid userId)
    {
        var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(CoachingApiFactory.JwtSecret));
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.NameId, userId.ToString()),
            new Claim(ClaimTypes.Email, "coach@test.com"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var token = new JwtSecurityToken(
            issuer: CoachingApiFactory.JwtIssuer,
            audience: CoachingApiFactory.JwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
    }
}
