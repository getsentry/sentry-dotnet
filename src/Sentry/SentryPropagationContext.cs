namespace Sentry;

internal class SentryPropagationContext
{
    public SentryId TraceId { get; }
    public SpanId SpanId { get; }
    public SpanId? ParentSpanId { get; }
    public DynamicSamplingContext? DynamicSamplingContext { get; }

    private SentryPropagationContext(
        SentryTraceHeader traceHeader,
        SpanId? parentSpanId,
        DynamicSamplingContext? dynamicSamplingContext)
    {
        TraceId = traceHeader.TraceId;
        SpanId = traceHeader.SpanId;
        ParentSpanId = parentSpanId;
        DynamicSamplingContext = dynamicSamplingContext;
    }

    public SentryPropagationContext()
    {
        TraceId = new SentryId();
        SpanId = new SpanId();
    }

    public static SentryPropagationContext CreateFromHeaders(string? sentryTraceHeader, string? baggageHeadersString)
    {
        if (sentryTraceHeader == null)
        {
            return new SentryPropagationContext();
        }

        var traceHeader = SentryTraceHeader.Parse(sentryTraceHeader);

        DynamicSamplingContext? dynamicSamplingContext = null;
        if (baggageHeadersString is not null)
        {
            var baggageHeader = BaggageHeader.TryParse(baggageHeadersString);
            dynamicSamplingContext = baggageHeader?.CreateDynamicSamplingContext();
        }

        return new SentryPropagationContext(traceHeader, null, dynamicSamplingContext);
    }
}
