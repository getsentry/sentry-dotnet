namespace Sentry.Tests.Protocol;

public class SdkVersionTests
{
    private readonly IDiagnosticLogger _testOutputLogger;

    public SdkVersionTests(ITestOutputHelper output)
    {
        _testOutputLogger = new TestOutputDiagnosticLogger(output);
    }

    [Fact]
    public void InstanceIsCorrect()
    {
        var instance = SdkVersion.Instance;
        Assert.Equal("sentry.dotnet", instance.Name);
        Assert.NotNull(instance.Version);
        Assert.NotEmpty(instance.Version);
    }

    [Fact]
    public void AddPackage_DoesNotExcludeCurrentOne()
    {
        var sut = new SdkVersion();

        sut.AddPackage("Sentry", "1.0");
        sut.AddPackage("Sentry.Log4Net", "10.0");

        Assert.Equal(2, sut.Packages.Count());
    }

    [Fact]
    public void SerializeObject_AllPropertiesSetToNonDefault_SerializesValidObject()
    {
        var sut = new SdkVersion
        {
            Name = "Sentry.Test.SDK",
            Version = "0.0.1-preview1",
        };
        sut.AddPackage("Sentry.AspNetCore", "2.0");
        sut.AddPackage("Sentry", "1.0");

        var actual = sut.ToJsonString(_testOutputLogger, indented: true);

        Assert.Equal("""
            {
              "packages": [
                {
                  "name": "Sentry",
                  "version": "1.0"
                },
                {
                  "name": "Sentry.AspNetCore",
                  "version": "2.0"
                }
              ],
              "name": "Sentry.Test.SDK",
              "version": "0.0.1-preview1"
            }
            """,
            actual);
    }

    [Theory]
    [MemberData(nameof(TestCases))]
    public void SerializeObject_TestCase_SerializesAsExpected((SdkVersion sdkVersion, string serialized) @case)
    {
        var actual = @case.sdkVersion.ToJsonString(_testOutputLogger);

        Assert.Equal(@case.serialized, actual);
    }

    public static IEnumerable<object[]> TestCases()
    {
        yield return new object[] { (new SdkVersion(), "{}") };
        yield return new object[] { (new SdkVersion { Name = "some name" }, """{"name":"some name"}""") };
        yield return new object[] { (new SdkVersion { Version = "some version" }, """{"version":"some version"}""") };
        var sdk = new SdkVersion();
        sdk.AddPackage("b", "2");
        sdk.AddPackage("a", "1");
        yield return new object[] { (sdk, """{"packages":[{"name":"a","version":"1"},{"name":"b","version":"2"}]}""") };
    }

    [Fact]
    public void SerializeObject_IgnoresDuplicatePackages()
    {
        var sdkVersion = new SdkVersion
        {
            Name = "Sentry.Test.SDK",
            Version = "3.9.2"
        };
        sdkVersion.AddPackage("Foo", "Alpha");
        sdkVersion.AddPackage("Bar", "Beta");
        sdkVersion.AddPackage("Foo", "Alpha");
        sdkVersion.AddPackage("Bar", "Beta");
        var actual = sdkVersion.ToJsonString(_testOutputLogger);
        var expected = TrimJson(@"
{
   ""packages"": [
        {
            ""name"": ""Bar"",
            ""version"": ""Beta""
        },
        {
            ""name"": ""Foo"",
            ""version"": ""Alpha""
        }
    ],
    ""name"": ""Sentry.Test.SDK"",
    ""version"": ""3.9.2""
}");
        Assert.Equal(expected, actual);
    }

    private static string TrimJson(string json)
    {
        return Regex.Replace(json, @"\s", "");
    }

}
