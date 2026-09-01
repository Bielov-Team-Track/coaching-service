using Coaching.Application.RichText;
using FluentAssertions;

namespace Coaching.Tests.Unit.RichText;

/// <summary>
/// The instructions carry the dials as {tokens}. Scanning them is how the server checks the
/// client's splice against the dial list — the one thing standing between a coach and a drill
/// that reads "Serve {reps} times" out loud.
/// </summary>
[TestFixture]
[Category("Unit")]
public class DialTokensTests
{
    [Test]
    public void Scan_FindsEveryTokenInTheOrderItAppears()
    {
        var found = DialTokens.Scan(["Serve {reps} times at {tempo} tempo", "Then {reps} more"]);

        found.Should().ContainInOrder("reps", "tempo");
        found.Should().HaveCount(2, "a token named twice is still one dial");
    }

    [Test]
    public void Scan_IgnoresThePluralMarkerThatFollowsANumber()
    {
        var found = DialTokens.Scan(["Do {reps} rep~s and {sets} set~es"]);

        found.Should().Equal("reps", "sets");
    }

    [TestCase("{}")]
    [TestCase("{ reps }")]
    [TestCase("{2reps}")]
    [TestCase("{reps-fast}")]
    [TestCase("{reps.fast}")]
    public void Scan_SkipsBracesThatAreNotTokens(string braces)
    {
        DialTokens.Scan([$"Serve {braces} times"]).Should().BeEmpty();
    }

    [Test]
    public void Scan_TellsTokensApartByCase()
    {
        // The client splices on the literal name, so {Reps} and {reps} are two different words.
        DialTokens.Scan(["{Reps} then {reps}"]).Should().Equal("Reps", "reps");
    }

    [Test]
    public void Reconcile_ReportsATokenNoDialDefines()
    {
        var (unknown, unused) = DialTokens.Reconcile(["Serve {reps} at {tempo}"], ["reps"]);

        unknown.Should().Equal("tempo");
        unused.Should().BeEmpty();
    }

    [Test]
    public void Reconcile_ReportsADialTheProseNoLongerMentions()
    {
        var (unknown, unused) = DialTokens.Reconcile(["Serve {reps} times"], ["reps", "tempo"]);

        unknown.Should().BeEmpty();
        unused.Should().Equal("tempo");
    }

    [Test]
    public void Reconcile_WhenTheProseAndTheDialsAgree_ReportsNothing()
    {
        var (unknown, unused) = DialTokens.Reconcile(["Serve {reps} at {tempo}"], ["tempo", "reps"]);

        unknown.Should().BeEmpty();
        unused.Should().BeEmpty();
    }

    [TestCase("reps", true)]
    [TestCase("repsPerSet", true)]
    [TestCase("reps2", true)]
    [TestCase("2reps", false)]
    [TestCase("reps set", false)]
    [TestCase("reps_set", false)]
    [TestCase("", false)]
    [TestCase(null, false)]
    public void IsValidName_AcceptsOnlyWhatTheProseCanCarry(string? name, bool expected)
    {
        DialTokens.IsValidName(name).Should().Be(expected);
    }

    [Test]
    public void IsValidName_RejectsALeadingCapitalBecauseTheWireWouldLowercaseIt()
    {
        // The name is a JSON object key, and the API camelCases those. See DialValueWireTests.
        DialTokens.IsValidName("Reps").Should().BeFalse();
    }

    [Test]
    public void IsValidName_RejectsANameTooLongToStore()
    {
        DialTokens.IsValidName(new string('a', 61)).Should().BeFalse();
    }
}
