namespace Sentry.Protocol.Spans;

internal static class SpanV2Extensions
{
    /// <summary>
    /// Quick and dirty batching mechanism to ensure we don't exceed the maximum number of spans per envelope when
    /// sending SpanV2s them to Sentry. This is temporary - we'll remove it once we implement a Span Buffer or a
    /// Telemetry Processor.
    /// </summary>
    public static IEnumerable<IReadOnlyCollection<SpanV2>> QuickBatch(this IEnumerable<SpanV2> spans)
    {
        var batch = new List<SpanV2>(SpanV2.MaxSpansPerEnvelope);
        foreach (var span in spans)
        {
            batch.Add(span);
            if (batch.Count == SpanV2.MaxSpansPerEnvelope)
            {
                yield return batch;
                batch = new List<SpanV2>(SpanV2.MaxSpansPerEnvelope);
            }
        }
        if (batch.Count > 0)
        {
            yield return batch;
        }
    }
}
