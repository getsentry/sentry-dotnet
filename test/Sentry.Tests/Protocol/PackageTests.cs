namespace Sentry.Tests.Protocol;

public class PackageTests
{
    private readonly IDiagnosticLogger _testOutputLogger;

    public PackageTests(ITestOutputHelper output)
    {
        _testOutputLogger = new TestOutputDiagnosticLogger(output);
    }

    [Fact]
    public void SerializeObject_AllPropertiesSetToNonDefault_SerializesValidObject()
    {
        var sut = new SentryPackage("nuget:Sentry", "1.0.0-preview");

        var actual = sut.ToJsonString(_testOutputLogger);

        Assert.Equal("""{"name":"nuget:Sentry","version":"1.0.0-preview"}""", actual);
    }

    [Theory]
    [MemberData(nameof(TestCases))]
    public void SerializeObject_TestCase_SerializesAsExpected((SentryPackage msg, string serialized) @case)
    {
        var actual = @case.msg.ToJsonString(_testOutputLogger);

        Assert.Equal(@case.serialized, actual);
    }

    public static IEnumerable<object[]> TestCases()
    {
        yield return new object[] { (new SentryPackage(null, null), "{}") };
        yield return new object[] { (new SentryPackage("nuget:Sentry", null), """{"name":"nuget:Sentry"}""") };
        yield return new object[] { (new SentryPackage(null, "0.0.0-alpha"), """{"version":"0.0.0-alpha"}""") };
    }
}
