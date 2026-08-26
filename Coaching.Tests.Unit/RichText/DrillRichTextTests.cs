using Coaching.Application.RichText;
using FluentAssertions;

namespace Coaching.Tests.Unit.RichText;

[TestFixture]
[Category("Unit")]
public class DrillRichTextTests
{
    [Test]
    public void Sanitize_StripsScriptsAndKeepsFormatting()
    {
        const string hostile = "<ol><li><p>Serve <strong>deep</strong><script>alert(1)</script></p></li></ol>";

        var result = DrillRichText.Sanitize(hostile);

        result.Should().NotContain("script");
        result.Should().Contain("<strong>deep</strong>");
    }

    [Test]
    public void Sanitize_DropsJavascriptHrefButKeepsTheText()
    {
        var result = DrillRichText.Sanitize("<p><a href=\"javascript:alert(1)\">tap</a></p>");

        result.Should().NotContain("javascript:");
        result.Should().Contain("tap");
    }

    [Test]
    public void Sanitize_TreatsAnEmptyEditorDocumentAsNull()
    {
        // What the editor sends when a coach opens the field and types nothing.
        DrillRichText.Sanitize("<ol><li><p></p></li></ol>").Should().BeNull();
        DrillRichText.Sanitize("   ").Should().BeNull();
        DrillRichText.Sanitize(null).Should().BeNull();
    }

    [Test]
    public void ToLines_GivesOneLinePerListItem()
    {
        const string html = "<ol><li><p>First</p></li><li><p>Second</p></li></ol>";

        DrillRichText.ToLines(html).Should().Equal("First", "Second");
    }

    [Test]
    public void ToLines_DoesNotDuplicateAListItemsOwnParagraph()
    {
        // li and p both match the selector; the item must still yield a single line.
        DrillRichText.ToLines("<ul><li><p>Only once</p></li></ul>").Should().Equal("Only once");
    }

    [Test]
    public void ToLines_SkipsBlankBlocks()
    {
        DrillRichText.ToLines("<ol><li><p>Kept</p></li><li><p>  </p></li></ol>").Should().Equal("Kept");
    }

    [Test]
    public void FromLines_WrapsLegacyLinesAsAList()
    {
        var html = DrillRichText.FromLines(["First", "Second"], ordered: true);

        html.Should().Be("<ol><li><p>First</p></li><li><p>Second</p></li></ol>");
    }

    [Test]
    public void FromLines_EscapesMarkupInStoredText()
    {
        var html = DrillRichText.FromLines(["5 < 6 & rising"], ordered: false);

        html.Should().Be("<ul><li><p>5 &lt; 6 &amp; rising</p></li></ul>");
    }

    [Test]
    public void FromLines_ReturnsNullWhenThereIsNothingToWrap()
    {
        DrillRichText.FromLines([], ordered: true).Should().BeNull();
        DrillRichText.FromLines(["   "], ordered: true).Should().BeNull();
    }

    [Test]
    public void RoundTrip_LegacyLinesSurviveBothDirections()
    {
        string[] original = ["Form two groups", "Serve to zones"];

        var lines = DrillRichText.ToLines(DrillRichText.FromLines(original, ordered: true));

        lines.Should().Equal(original);
    }
}
