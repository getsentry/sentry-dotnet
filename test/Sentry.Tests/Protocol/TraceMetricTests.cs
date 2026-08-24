namespace Sentry.Tests.Protocol;

/// <summary>
/// See <see href="https://develop.sentry.dev/sdk/telemetry/metrics/"/>.
/// See also <see cref="Sentry.Tests.SentryMetricTests"/>.
/// </summary>
public class TraceMetricTests
{
    private readonly TestOutputDiagnosticLogger _output;

    public TraceMetricTests(ITestOutputHelper output)
    {
        _output = new TestOutputDiagnosticLogger(output);
    }

    [Fact]
    public void Type_IsAssignableFrom_ISentryJsonSerializable()
    {
        var metric = new TraceMetric([]);

        Assert.IsAssignableFrom<ISentryJsonSerializable>(metric);
    }

    [Fact]
    public void Length_One_Single()
    {
        var metric = new TraceMetric([CreateMetric()]);

        var length = metric.Length;

        Assert.Equal(1, length);
    }

    [Fact]
    public void Items_One_Single()
    {
        var metric = new TraceMetric([CreateMetric()]);

        var items = metric.Items;

        Assert.Equal(1, items.Length);
    }

    [Fact]
    public void WriteTo_Empty_AsJson()
    {
        var metric = new TraceMetric([]);

        var document = metric.ToJsonDocument(_output);

        Assert.Equal("""{"items":[]}""", document.RootElement.ToString());
    }

    private static SentryMetric<int> CreateMetric()
    {
        return new SentryMetric<int>(DateTimeOffset.MinValue, SentryId.Empty, SentryMetricType.Counter, "sentry_tests.trace_metric_tests.counter", 1);
    }
}
