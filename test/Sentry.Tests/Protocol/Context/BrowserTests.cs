// ReSharper disable once CheckNamespace

using Sentry.Testing;

namespace Sentry.Protocol.Tests.Context;

public class BrowserTests
{
    private readonly IDiagnosticLogger _testOutputLogger;

    public BrowserTests(ITestOutputHelper output)
    {
        _testOutputLogger = new TestOutputDiagnosticLogger(output);
    }

    [Fact]
    public void SerializeObject_AllPropertiesSetToNonDefault_SerializesValidObject()
    {
        var sut = new Browser
        {
            Version = "6",
            Name = "Internet Explorer",
        };

        var actualString = sut.ToJsonString(_testOutputLogger);

        var actual = Json.Parse(actualString, Browser.FromJson);
        actual.Should().BeEquivalentTo(sut);
    }

    [Fact]
    public void Clone_CopyValues()
    {
        var sut = new Browser
        {
            Name = "name",
            Version = "version"
        };

        var clone = sut.Clone();

        Assert.Equal(sut.Name, clone.Name);
        Assert.Equal(sut.Version, clone.Version);
    }

    [Theory]
    [MemberData(nameof(TestCases))]
    public void SerializeObject_TestCase_SerializesAsExpected((Browser browser, string serialized) @case)
    {
        var actual = @case.browser.ToJsonString(_testOutputLogger);

        Assert.Equal(@case.serialized, actual);
    }

    public static IEnumerable<object[]> TestCases()
    {
        yield return new object[] { (new Browser(), "{\"type\":\"browser\"}") };
        yield return new object[] { (new Browser { Name = "some name" }, "{\"type\":\"browser\",\"name\":\"some name\"}") };
        yield return new object[] { (new Browser { Version = "some version" }, "{\"type\":\"browser\",\"version\":\"some version\"}") };
    }
}
