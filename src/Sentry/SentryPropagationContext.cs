using Sentry.Extensibility;

namespace Sentry;

internal class SentryPropagationContext
{
    public SentryId TraceId { get; }
    public SpanId SpanId { get; }
    public SpanId? ParentSpanId { get; }
    public DynamicSamplingContext? DynamicSamplingContext { get; set; }

    private SentryPropagationContext(
        SentryId traceId,
        SpanId parentSpanId,
        DynamicSamplingContext? dynamicSamplingContext)
    {
        TraceId = traceId;
        SpanId = SpanId.Create();
        ParentSpanId = parentSpanId;
        DynamicSamplingContext = dynamicSamplingContext;
    }

    public SentryPropagationContext()
    {
        TraceId = SentryId.Create();
        SpanId = SpanId.Create();
    }

    public static SentryPropagationContext CreateFromHeaders(IDiagnosticLogger? logger, SentryTraceHeader? traceHeader, BaggageHeader? baggageHeader)
    {
        if (traceHeader == null)
        {
            return new SentryPropagationContext();
        }

        try
        {
            DynamicSamplingContext? dynamicSamplingContext = null;
            if (baggageHeader is not null)
            {
                dynamicSamplingContext = baggageHeader.CreateDynamicSamplingContext();
            }

            return new SentryPropagationContext(traceHeader.TraceId, traceHeader.SpanId, dynamicSamplingContext);
        }
        catch (Exception e)
        {
            logger?.LogError(
                "Failed to create the 'SentryPropagationContext' from provided headers: '{0}', '{1}'. " +
                        "\nFalling back to new PropagationContext.", e, traceHeader, baggageHeader);
            return new SentryPropagationContext();
        }
    }
}
