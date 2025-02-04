using System.Text.Json.Nodes;

namespace Sentry.Tests;

public partial class SerializationTests
{
    private readonly IDiagnosticLogger _testOutputLogger;

    public SerializationTests(ITestOutputHelper output)
    {
        _testOutputLogger = new TestOutputDiagnosticLogger(output);
    }

    [Fact]
    public void Serialization_TransactionAndSpanData()
    {
        var hub = Substitute.For<IHub>();
        var context = new TransactionContext("name", "operation", new SentryTraceHeader(SentryId.Empty, SpanId.Empty, false));
        var transactionTracer = new TransactionTracer(hub, context);
        var span = transactionTracer.StartChild("childop");
        span.SetExtra("span1", "value1");

        var transaction = new SentryTransaction(transactionTracer)
        {
            IsSampled = false
        };
        transaction.SetExtra("transaction1", "transaction_value");
        var json = transaction.ToJsonString(_testOutputLogger);
        _testOutputLogger.LogDebug(json);

        var node = JsonNode.Parse(json);
        var dataNode = node?["contexts"]?["trace"]?["data"]?["transaction1"]?.GetValue<string>();
        dataNode.Should().NotBeNull("contexts.trace.data.transaction1 not found");
        dataNode.Should().Be("transaction_value");

        var spansNode = node?["spans"]?.AsArray();
        spansNode.Should().NotBeNull("spans not found");
        var spanDataNode = spansNode!.FirstOrDefault()?["data"]?["span1"]?.GetValue<string>();
        spanDataNode.Should().NotBeNull("spans.data not found");
        spanDataNode.Should().Be("value1");

        // verify deserialization
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
        var el = JsonElement.ParseValue(ref reader);
        var backTransaction = SentryTransaction.FromJson(el);

        backTransaction.Spans.First().Extra["span1"].Should().Be("value1", "Span value missing");
        backTransaction.Contexts.Trace.Extra["transaction1"].Should().Be("transaction_value", "Transaction value missing");
    }
}
