namespace Sentry.Tests.Internals;

public class ClientReportTests
{
    private readonly IDiagnosticLogger _testOutputLogger;
    private readonly ClientReport _testClientReport;
    private const string TestJsonString =
        "{" +
        "\"timestamp\":\"9999-12-31T23:59:59.9999999+00:00\"," + "\"discarded_events\":[" +
        "{\"reason\":\"before_send\",\"category\":\"attachment\",\"quantity\":1}," +
        "{\"reason\":\"cache_overflow\",\"category\":\"error\",\"quantity\":2}," +
        "{\"reason\":\"event_processor\",\"category\":\"security\",\"quantity\":3}]" +
        "}";

    public ClientReportTests(ITestOutputHelper output)
    {
        _testOutputLogger = new TestOutputDiagnosticLogger(output);

        var timestamp = DateTimeOffset.MaxValue;
        var discardedEvents = new Dictionary<DiscardReasonWithCategory, int>
        {
            {DiscardReason.BeforeSend.WithCategory(DataCategory.Attachment), 1},
            {DiscardReason.CacheOverflow.WithCategory(DataCategory.Error), 2},
            {DiscardReason.EventProcessor.WithCategory(DataCategory.Security), 3}
        };
        _testClientReport = new ClientReport(timestamp, discardedEvents);
    }

    [Fact]
    public void Serializes()
    {
        var jsonString = _testClientReport.ToJsonString(_testOutputLogger);
        Assert.Equal(TestJsonString, jsonString);
    }

    [Fact]
    public void Deserializes()
    {
        var element = JsonDocument.Parse(TestJsonString).RootElement;
        var clientReport = ClientReport.FromJson(element);
        clientReport.Should().BeEquivalentTo(_testClientReport);
    }
}
