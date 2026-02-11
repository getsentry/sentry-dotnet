using Sentry.Protocol.Spans;

namespace Sentry.Tests.Protocol.Envelopes;

public class SpanV2EnvelopeTests
{
    [Fact]
    public async Task EnvelopeItem_FromSpanV2_SerializesHeaderWithContentTypeAndItemCount()
    {
        var span = new SpanV2(SentryId.Parse("0123456789abcdef0123456789abcdef"), SpanId.Parse("0123456789abcdef"), "db", DateTimeOffset.Parse("2020-01-01T00:00:00Z"))
        {
            Description = "select 1",
            Status = SpanStatus.Ok,
            EndTimestamp = DateTimeOffset.Parse("2020-01-01T00:00:01Z"),
        };

        using var envelopeItem = EnvelopeItem.FromSpanV2(span);

        using var stream = new MemoryStream();
        await envelopeItem.SerializeAsync(stream, null);
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var output = await reader.ReadToEndAsync();

        var firstLine = output.Split('\n', StringSplitOptions.RemoveEmptyEntries)[0];
        firstLine.Should().Contain("\"type\":\"span_v2\"");
        firstLine.Should().Contain("\"item_count\":1");
        firstLine.Should().Contain("\"content_type\":\"application/vnd.sentry.items.span\\u002Bjson\"");
    }

    [Fact]
    public void Envelope_FromSpanV2_ThrowsWhenMoreThan100Spans()
    {
        var spans = Enumerable.Range(0, 101).Select(_ =>
            new SpanV2(SentryId.Create(), SpanId.Create(), "op", DateTimeOffset.UtcNow));

        Action act = () => Envelope.FromSpanV2(spans);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Envelope_FromSpanV2_CreatesUpTo100Items()
    {
        var spans = Enumerable.Range(0, 100).Select(_ =>
            new SpanV2(SentryId.Create(), SpanId.Create(), "op", DateTimeOffset.UtcNow)).ToList();

        using var envelope = Envelope.FromSpanV2(spans);

        envelope.Items.Should().HaveCount(100);
        envelope.Items.All(i => i.TryGetType() == "span_v2").Should().BeTrue();
    }
}
