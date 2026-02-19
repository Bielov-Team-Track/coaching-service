using Shared.Logging.Redaction;

namespace Shared.Tests.Logging.Redaction;

public class PiiRedactorTests
{
    private PiiRedactor _redactor;

    [SetUp]
    public void Setup()
    {
        _redactor = new PiiRedactor();
    }

    [TestCase("john.doe@example.com", "joh********@example.com")]
    [TestCase("test", "tes********")]
    [TestCase("ab", "********")]
    [TestCase("", "")]
    [TestCase("1234567890", "123********")]
    public void Should_Redact_Correctly(string input, string expectedResult)
    {
        var source = input.AsSpan();
        var buffer = new char[_redactor.GetRedactedLength(source)];
        var length = _redactor.Redact(source, buffer);
        var actualResult = new string(buffer, 0, length);

        Assert.That(actualResult, Is.EqualTo(expectedResult));
    }

    [Test]
    public void Should_Handle_Small_Destination_Buffer()
    {
        var source = "john.doe@example.com".AsSpan();
        var smallBuffer = new char[5]; // Smaller than needed

        var written = _redactor.Redact(source, smallBuffer);

        Assert.That(written, Is.EqualTo(5));
        Assert.That(new string(smallBuffer, 0, written), Is.EqualTo("joh**"));
    }
}