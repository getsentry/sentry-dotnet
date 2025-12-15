namespace Sentry.Tests.Protocol;

public class SentryThreadTests
{
    private readonly IDiagnosticLogger _testOutputLogger;

    public SentryThreadTests(ITestOutputHelper output)
    {
        _testOutputLogger = new TestOutputDiagnosticLogger(output);
    }

    [Fact]
    public void SerializeObject_AllPropertiesSetToNonDefault_SerializesValidObject()
    {
        var sut = new SentryThread
        {
            Crashed = true,
            Current = true,
            Main = true,
            Id = 0,
            Name = "thread11",
            Stacktrace = new SentryStackTrace
            {
                Frames = { new SentryStackFrame
                {
                    FileName = "test"
                }}
            }
        };

        var actual = sut.ToJsonString(_testOutputLogger, indented: true);

        Assert.Equal("""
            {
              "id": 0,
              "name": "thread11",
              "crashed": true,
              "current": true,
              "main": true,
              "stacktrace": {
                "frames": [
                  {
                    "filename": "test"
                  }
                ]
              }
            }
            """,
            actual);
    }

    [Theory]
    [MemberData(nameof(TestCases))]
    public void SerializeObject_TestCase_SerializesAsExpected((SentryThread thread, string serialized) @case)
    {
        var actual = @case.thread.ToJsonString(_testOutputLogger);

        Assert.Equal(@case.serialized, actual);
    }

    public static IEnumerable<object[]> TestCases()
    {
        yield return new object[] { (new SentryThread(), "{}") };
        yield return new object[] { (new SentryThread { Name = "some name" }, """{"name":"some name"}""") };
        yield return new object[] { (new SentryThread { Crashed = false }, """{"crashed":false}""") };
        yield return new object[] { (new SentryThread { Current = false }, """{"current":false}""") };
        yield return new object[] { (new SentryThread { Main = false }, """{"main":false}""") };
        yield return new object[] { (new SentryThread { Main = true }, """{"main":true}""") };
        yield return new object[] { (new SentryThread { Id = 200 }, """{"id":200}""") };
        yield return new object[] { (new SentryThread { Stacktrace = new SentryStackTrace { Frames = { new SentryStackFrame { InApp = true } } } },
            """{"stacktrace":{"frames":[{"in_app":true}]}}""") };
    }
}
