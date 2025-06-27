namespace Sentry.Tests.Protocol;

/// <summary>
/// See <see href="https://develop.sentry.dev/sdk/telemetry/logs/"/>.
/// See also <see cref="Sentry.Tests.SentryLogTests"/>.
/// </summary>
public class StructuredLogTests
{
    private readonly TestOutputDiagnosticLogger _output;

    public StructuredLogTests(ITestOutputHelper output)
    {
        _output = new TestOutputDiagnosticLogger(output);
    }

    [Fact]
    public void Type_IsAssignableFrom_ISentryJsonSerializable()
    {
        var log = new StructuredLog([]);

        Assert.IsAssignableFrom<ISentryJsonSerializable>(log);
    }

    [Fact]
    public void Length_One_Single()
    {
        var log = new StructuredLog([CreateLog()]);

        var length = log.Length;

        Assert.Equal(1, length);
    }

    [Fact]
    public void Items_One_Single()
    {
        var log = new StructuredLog([CreateLog()]);

        var items = log.Items;

        Assert.Equal(1, items.Length);
    }

    [Fact]
    public void WriteTo_Empty_AsJson()
    {
        var log = new StructuredLog([]);

        var document = log.ToJsonDocument(_output);

        Assert.Equal("""{"items":[]}""", document.RootElement.ToString());
    }

    private static SentryLog CreateLog()
    {
        return new SentryLog(DateTimeOffset.MinValue, SentryId.Empty, SentryLogLevel.Trace, "message");
    }
}
