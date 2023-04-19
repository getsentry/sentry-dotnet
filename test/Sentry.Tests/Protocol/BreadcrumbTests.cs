namespace Sentry.Tests.Protocol;

public class BreadcrumbTests : ImmutableTests<Breadcrumb>
{
    private readonly IDiagnosticLogger _testOutputLogger;

    public BreadcrumbTests(ITestOutputHelper output)
    {
        _testOutputLogger = new TestOutputDiagnosticLogger(output);
    }

    [Fact]
    public void SerializeObject_ParameterlessConstructor_IncludesTimestamp()
    {
        var sut = new Breadcrumb("test", "unit");

        var actualJson = sut.ToJsonString(_testOutputLogger);
        var actual = Json.Parse(actualJson, Breadcrumb.FromJson);

        Assert.NotEqual(default, actual.Timestamp);
    }

    [Fact]
    public void SerializeObject_AllPropertiesSetToNonDefault_SerializesValidObject()
    {
        var sut = new Breadcrumb(
            DateTimeOffset.MaxValue,
            "message1",
            "type1",
            new Dictionary<string, string> { { "key", "val" } },
            "category1",
            BreadcrumbLevel.Warning);

        var actual = sut.ToJsonString(_testOutputLogger, indented: true);

        Assert.Equal("""
            {
              "timestamp": "9999-12-31T23:59:59.999Z",
              "message": "message1",
              "type": "type1",
              "data": {
                "key": "val"
              },
              "category": "category1",
              "level": "warning"
            }
            """,
            actual);
    }

    [Theory]
    [MemberData(nameof(TestCases))]
    public void SerializeObject_TestCase_SerializesAsExpected((Breadcrumb breadcrumb, string serialized) @case)
    {
        var actual = @case.breadcrumb.ToJsonString(_testOutputLogger);

        Assert.Equal(@case.serialized, actual);
    }

    public static IEnumerable<object[]> TestCases()
    {
        yield return new object[] { (new Breadcrumb(DateTimeOffset.MaxValue), """{"timestamp":"9999-12-31T23:59:59.999Z"}""") };
        yield return new object[] { (new Breadcrumb(DateTimeOffset.MaxValue, "message"), """{"timestamp":"9999-12-31T23:59:59.999Z","message":"message"}""") };
        yield return new object[] { (new Breadcrumb(DateTimeOffset.MaxValue, type: "type"), """{"timestamp":"9999-12-31T23:59:59.999Z","type":"type"}""") };
        yield return new object[] { (new Breadcrumb(DateTimeOffset.MaxValue, data: new Dictionary<string, string> { { "key", "val" } }), """{"timestamp":"9999-12-31T23:59:59.999Z","data":{"key":"val"}}""") };
        yield return new object[] { (new Breadcrumb(DateTimeOffset.MaxValue, category: "category"), """{"timestamp":"9999-12-31T23:59:59.999Z","category":"category"}""") };
        yield return new object[] { (new Breadcrumb(DateTimeOffset.MaxValue, level: BreadcrumbLevel.Critical), """{"timestamp":"9999-12-31T23:59:59.999Z","level":"critical"}""") };
    }
}
