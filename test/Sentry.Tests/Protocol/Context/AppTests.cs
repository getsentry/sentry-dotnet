namespace Sentry.Tests.Protocol.Context;

public class AppTests
{
    private readonly IDiagnosticLogger _testOutputLogger;

    public AppTests(ITestOutputHelper output)
    {
        _testOutputLogger = new TestOutputDiagnosticLogger(output);
    }

    [Fact]
    public void SerializeObject_AllPropertiesSetToNonDefault_SerializesValidObject()
    {
        var sut = new App
        {
            Version = "8b03fd7",
            Build = "1.23152",
            BuildType = "nightly",
            Hash = "93fd0e9a",
            Name = "Sentry.Test.App",
            StartTime = DateTimeOffset.MaxValue,
            InForeground = true
        };

        var actualString = sut.ToJsonString(_testOutputLogger);

        var actual = Json.Parse(actualString, App.FromJson);
        actual.Should().BeEquivalentTo(sut);
    }

    [Fact]
    public void Clone_CopyValues()
    {
        var sut = new App
        {
            Build = "build",
            BuildType = "build type",
            Hash = "hash",
            Identifier = "identifier",
            Name = "name",
            StartTime = DateTimeOffset.UtcNow,
            Version = "version",
            InForeground = false
        };

        var clone = sut.Clone();

        Assert.Equal(sut.Build, clone.Build);
        Assert.Equal(sut.BuildType, clone.BuildType);
        Assert.Equal(sut.Hash, clone.Hash);
        Assert.Equal(sut.Identifier, clone.Identifier);
        Assert.Equal(sut.Name, clone.Name);
        Assert.Equal(sut.StartTime, clone.StartTime);
        Assert.Equal(sut.Version, clone.Version);
        Assert.Equal(sut.InForeground, clone.InForeground);
    }

    [Theory]
    [MemberData(nameof(TestCases))]
    public void SerializeObject_TestCase_SerializesAsExpected((App app, string serialized) @case)
    {
        var actual = @case.app.ToJsonString(_testOutputLogger);

        Assert.Equal(@case.serialized, actual);
    }

    public static IEnumerable<object[]> TestCases()
    {
        yield return new object[] { (new App(), """{"type":"app"}""") };
        yield return new object[] { (new App { Name = "some name" }, """{"type":"app","app_name":"some name"}""") };
        yield return new object[] { (new App { Build = "some build" }, """{"type":"app","app_build":"some build"}""") };
        yield return new object[] { (new App { BuildType = "some build type" }, """{"type":"app","build_type":"some build type"}""") };
        yield return new object[] { (new App { Hash = "some hash" }, """{"type":"app","device_app_hash":"some hash"}""") };
        yield return new object[] { (new App { StartTime = DateTimeOffset.MaxValue }, """{"type":"app","app_start_time":"9999-12-31T23:59:59.9999999+00:00"}""") };
        yield return new object[] { (new App { Version = "some version" }, """{"type":"app","app_version":"some version"}""") };
        yield return new object[] { (new App { Identifier = "some identifier" }, """{"type":"app","app_identifier":"some identifier"}""") };
        yield return new object[] { (new App { InForeground = true }, """{"type":"app","in_foreground":true}""") };
    }
}
