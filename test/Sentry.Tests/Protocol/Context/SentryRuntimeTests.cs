namespace Sentry.Tests.Protocol.Context;

public class SentryRuntimeTests
{
    private readonly IDiagnosticLogger _testOutputLogger;

    public SentryRuntimeTests(ITestOutputHelper output)
    {
        _testOutputLogger = new TestOutputDiagnosticLogger(output);
    }

    [Fact]
    public void SerializeObject_AllPropertiesSetToNonDefault_SerializesValidObject()
    {
        var sut = new Runtime
        {
            Version = "4.7.2",
            Name = ".NET Framework",
            Build = "461814",
            RawDescription = ".NET Framework 4.7.2"
        };

        var actual = sut.ToJsonString(_testOutputLogger, indented: true);

        Assert.Equal("""
            {
              "type": "runtime",
              "name": ".NET Framework",
              "version": "4.7.2",
              "raw_description": ".NET Framework 4.7.2",
              "build": "461814"
            }
            """, actual);
    }

    [Fact]
    public void Clone_CopyValues()
    {
        var sut = new Runtime
        {
            Name = "name",
            RawDescription = "RawDescription",
            Identifier = "identifier",
            Build = "build",
            Version = "version"
        };

        var clone = sut.Clone();

        Assert.Equal(sut.Name, clone.Name);
        Assert.Equal(sut.RawDescription, clone.RawDescription);
        Assert.Equal(sut.Identifier, clone.Identifier);
        Assert.Equal(sut.Build, clone.Build);
        Assert.Equal(sut.Version, clone.Version);
    }

    [Theory]
    [MemberData(nameof(TestCases))]
    public void SerializeObject_TestCase_SerializesAsExpected((Runtime runtime, string serialized) @case)
    {
        var actual = @case.runtime.ToJsonString(_testOutputLogger);

        Assert.Equal(@case.serialized, actual);
    }

    public static IEnumerable<object[]> TestCases()
    {
        yield return new object[] { (new Runtime(), """{"type":"runtime"}""") };
        yield return new object[] { (new Runtime { Name = "some name" }, """{"type":"runtime","name":"some name"}""") };
        yield return new object[] { (new Runtime { Version = "some version" }, """{"type":"runtime","version":"some version"}""") };
        yield return new object[] { (new Runtime { Identifier = "some identifier" }, """{"type":"runtime","identifier":"some identifier"}""") };
        yield return new object[] { (new Runtime { Build = "some build" }, """{"type":"runtime","build":"some build"}""") };
        yield return new object[] { (new Runtime { RawDescription = "some Name, some version" }, """{"type":"runtime","raw_description":"some Name, some version"}""") };
    }
}
