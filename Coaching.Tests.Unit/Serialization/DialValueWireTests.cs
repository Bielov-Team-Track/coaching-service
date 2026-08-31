using Coaching.Application.DTOs.Templates;
using Coaching.Application.RichText;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Coaching.Tests.Unit.Serialization;

/// <summary>
/// A dial's answers travel as a JSON object keyed by the dial's name, and the API's camelCase
/// resolver rewrites dictionary keys as well as property names. A name with a leading capital
/// would go out one way and come back another, and the client's splice on the original token
/// would quietly find nothing — no error, just a drill that reads "{Reps}" to the coach.
///
/// The grammar in DialTokens is what keeps that unreachable. These tests are the reason it is
/// narrower than it looks like it should be: widen it back to any letter and they fail.
/// </summary>
[TestFixture]
[Category("Unit")]
public class DialValueWireTests
{
    // The same shape Startup gives the controllers. Kept here rather than reached for, because
    // what is being pinned is the pair — the serializer and the names it is allowed to carry.
    private static readonly JsonSerializerSettings ApiSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
    };

    [TestCase("reps")]
    [TestCase("repsPerSet")]
    [TestCase("tempo2")]
    public void AValidDialNameSurvivesTheRoundTripUnchanged(string name)
    {
        // Arrange
        DialTokens.IsValidName(name).Should().BeTrue();
        var item = new PlanItemDto { Id = Guid.NewGuid(), DialValues = new() { [name] = "6" } };

        // Act
        var json = JsonConvert.SerializeObject(item, ApiSettings);
        var back = JsonConvert.DeserializeObject<PlanItemDto>(json, ApiSettings)!;

        // Assert
        back.DialValues.Should().ContainKey(name);
    }

    [Test]
    public void ANameWithALeadingCapitalWouldNotSurvive_WhichIsWhyItIsNotAName()
    {
        // Arrange — the failure this grammar exists to prevent, demonstrated rather than trusted
        var item = new PlanItemDto { Id = Guid.NewGuid(), DialValues = new() { ["Reps"] = "6" } };

        // Act
        var json = JsonConvert.SerializeObject(item, ApiSettings);

        // Assert
        json.Should().Contain("\"reps\":\"6\"").And.NotContain("\"Reps\"");
        DialTokens.IsValidName("Reps").Should().BeFalse();
    }
}
