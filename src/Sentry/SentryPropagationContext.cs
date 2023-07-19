namespace Sentry;

internal class SentryPropagationContext
{
    public SentryId TraceId { get; }
    public SpanId SpanId { get; }
    public SpanId? ParentSpanId { get; }
    public DynamicSamplingContext? DynamicSamplingContext { get; set; }

    private SentryPropagationContext(
        SentryTraceHeader traceHeader,
        SpanId? parentSpanId,
        DynamicSamplingContext? dynamicSamplingContext)
    {
        TraceId = traceHeader.TraceId;
        SpanId = SpanId.Create();
        ParentSpanId = parentSpanId ?? traceHeader.SpanId;
        DynamicSamplingContext = dynamicSamplingContext;
    }

    public SentryPropagationContext()
    {
        TraceId = SentryId.Create();
        SpanId = SpanId.Create();
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
