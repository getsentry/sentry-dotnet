namespace Sentry.Tests.Protocol;

public class SpanV2Tests
{
    [Fact]
    public async Task EnvelopeItem_FromSpans_SerializesHeaderWithContentTypeAndItemCount()
    {
        var span = new SpanV2(SentryId.Parse("0123456789abcdef0123456789abcdef"), SpanId.Parse("0123456789abcdef"), "db", DateTimeOffset.Parse("2020-01-01T00:00:00Z"))
        {
            Description = "select 1",
            Status = SpanStatus.Ok,
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
}
