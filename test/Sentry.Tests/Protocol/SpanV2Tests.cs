using Sentry.Protocol.Spans;

namespace Sentry.Tests.Protocol;

public class SpanV2Tests
{
    [Fact]
    public async Task EnvelopeItem_FromSpans_SerializesHeaderWithContentTypeAndItemCount()
    {
        var span = new SpanV2(SentryId.Parse("0123456789abcdef0123456789abcdef"), SpanId.Parse("0123456789abcdef"), "db", DateTimeOffset.Parse("2020-01-01T00:00:00Z"))
        {
            Name = "select 1",
            Status = SpanV2Status.Ok,
            EndTimestamp = DateTimeOffset.Parse("2020-01-01T00:00:01Z"),
        };

        var spanItems = new SpanV2Items([span]);
        using var envelopeItem = EnvelopeItem.FromSpans(spanItems);

        using var stream = new MemoryStream();
        await envelopeItem.SerializeAsync(stream, null);
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var output = await reader.ReadToEndAsync();

        var firstLine = output.Split('\n', StringSplitOptions.RemoveEmptyEntries)[0];
        firstLine.Should().Contain("\"type\":\"span\"");
        firstLine.Should().Contain("\"item_count\":1");
        firstLine.Should().Contain("\"content_type\":\"application/vnd.sentry.items.span\\u002Bjson\"");
    }

    [Fact]
    public void Envelope_FromSpans_CreatesSingleItem()
    {
        SpanV2[] spans = [new(SentryId.Create(), SpanId.Create(), "op", DateTimeOffset.UtcNow)];

        using var envelope = Envelope.FromSpans(spans);

        envelope.Items.Should().HaveCount(1);
        envelope.Items[0].TryGetType().Should().Be("span");
        envelope.Items[0].Header.GetValueOrDefault("item_count").Should().Be(1);
    }

    [Fact]
    public void Envelope_FromSpan_RespectsMaxSpans()
    {
        var spans = Enumerable.Range(0, SpanV2.MaxSpansPerEnvelope + 10)
            .Select(_ => new SpanV2(SentryId.Create(), SpanId.Create(), "op", DateTimeOffset.UtcNow))
            .ToArray();

        using var envelope = Envelope.FromSpans(spans);

        envelope.Items.Should().HaveCount(1);
        envelope.Items[0].TryGetType().Should().Be("span");
        envelope.Items[0].Header.GetValueOrDefault("item_count").Should().Be(SpanV2.MaxSpansPerEnvelope);
    }

    [Fact]
    public void SpanV2_FromTransaction_CopiesFields()
    {
        // Arrange
        const string name = "txn-name";
        const string operation = "txn-op";
        const string origin = "manual.test";
        var traceId = SentryId.Parse("0123456789abcdef0123456789abcdef");
        var spanId = SpanId.Parse("0123456789abcdef");
        var parentSpanId = SpanId.Parse("fedcba9876543210");
        var start = DateTimeOffset.Parse("2020-01-01T00:00:00Z");
        var end = DateTimeOffset.Parse("2020-01-01T00:00:01Z");
        const string tagKey = "tag-key";
        const string tagVal = "tag-val";
        const string dataKey = "data-key";
        const int dataVal = 123;

        var context = new TransactionContext(name, operation, spanId, parentSpanId, isSampled: true);
        var tracer = new TransactionTracer(DisabledHub.Instance, context);
        tracer.SetMeasurement("m", new Measurement(42, MeasurementUnit.Duration.Millisecond));
        tracer.Contexts.Trace.TraceId = traceId;
        tracer.Contexts.Trace.SpanId = spanId;
        tracer.Contexts.Trace.ParentSpanId = parentSpanId;
        tracer.Contexts.Trace.Operation = operation;
        tracer.StartTimestamp = start;
        tracer.EndTimestamp = end;
        tracer.Contexts.Trace.Status = SpanStatus.FailedPrecondition;
        tracer.Contexts.Trace.Origin = origin;
        tracer.SetTag(tagKey, tagVal);
        tracer.Contexts.Trace.SetData(dataKey, dataVal);

        var transaction = new SentryTransaction(tracer);

        var spanV2 = new SpanV2(transaction);

        using (new AssertionScope())
        {
            spanV2.TraceId.Should().Be(traceId);
            spanV2.SpanId.Should().Be(spanId);
            spanV2.ParentSpanId.Should().Be(parentSpanId);
            spanV2.Name.Should().Be(name);
            spanV2.Status.Should().Be(SpanV2Status.Error);
            spanV2.StartTimestamp.Should().Be(start);
            spanV2.EndTimestamp.Should().Be(end);

            // TODO: not yet sure if this is how they should be mapped from transaction properties.
            spanV2.Attributes.ShouldContain(SpanV2Attributes.Operation, operation);
            spanV2.Attributes.ShouldContain(SpanV2Attributes.Source, origin);
            spanV2.Attributes.ShouldContain(tagKey, tagVal);
            spanV2.Attributes.ShouldContain(dataKey, dataVal);
            // TODO: spanV2.Measurements.Should().ContainKey("m"); ???

            // TODO: Attachments - see https://develop.sentry.dev/sdk/telemetry/spans/span-protocol/#span-attachments
        }
    }

    [Fact]
    public void SpanV2_FromSpan_CopiesFields()
    {
        var txSpanId = SpanId.Parse("0123456789abcdef");
        var txParentSpanId = SpanId.Parse("fedcba9876543210");

        var parentSpanId = SpanId.Parse("fedcba9876543210");
        var traceId = SentryId.Parse("0123456789abcdef0123456789abcdef");
        const string description = "desc";
        const string operation = "db";
        const string origin = "test-origin";
        var start = DateTimeOffset.Parse("2020-01-02T00:00:00Z");
        var end = DateTimeOffset.Parse("2020-01-02T00:00:01Z");
        const string tagKey = "tag-key";
        const string tagVal = "tag-val";
        const string dataKey = "data-key";
        const int dataVal = 123;

        var context = new TransactionContext("Foo", "Bar", txSpanId, txParentSpanId, isSampled: true);
        var txTracer = new TransactionTracer(DisabledHub.Instance, context);
        var tracer = new SpanTracer(DisabledHub.Instance, txTracer, parentSpanId, traceId, operation);
        tracer.StartTimestamp = start;
        tracer.EndTimestamp = end;
        tracer.Status = SpanStatus.Cancelled;
        tracer.Description = description;
        tracer.Origin = origin;
        tracer.IsSampled = true;
        tracer.SetTag("tag-key", "tag-val");
        tracer.SetData("data-key", 123);
        tracer.SetMeasurement("m", new Measurement(42, MeasurementUnit.Duration.Millisecond));

        var span = new SentrySpan(tracer);


        var spanV2 = new SpanV2(span);

        using (new AssertionScope())
        {
            spanV2.TraceId.Should().Be(traceId);
            spanV2.SpanId.Should().Be(span.SpanId);
            spanV2.ParentSpanId.Should().Be(span.ParentSpanId);
            spanV2.Name.Should().Be(span.Description);
            spanV2.StartTimestamp.Should().Be(span.StartTimestamp);
            spanV2.EndTimestamp.Should().Be(span.EndTimestamp);
            spanV2.Status.Should().Be(SpanV2Status.Error);

            // TODO: not yet sure if this is how they should be mapped from transaction properties.
            spanV2.Attributes.ShouldContain(SpanV2Attributes.Operation, operation);
            spanV2.Attributes.ShouldContain(SpanV2Attributes.Source, origin);
            spanV2.Attributes.ShouldContain(tagKey, tagVal);
            spanV2.Attributes.ShouldContain(dataKey, dataVal);
            spanV2.Attributes.ShouldContain(SpanV2Attributes.Operation, operation);
            spanV2.Attributes.ShouldContain(SpanV2Attributes.Operation, operation);
            // TODO: spanV2.Measurements.Should().ContainKey("m"); ???
        }
    }
}
