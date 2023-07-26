using Sentry.Extensibility;

namespace Sentry;

internal class SentryPropagationContext
{
    public SentryId TraceId { get; }
    public SpanId SpanId { get; }
    public SpanId? ParentSpanId { get; }
    public bool? IsSampled { get; }
    public DynamicSamplingContext? DynamicSamplingContext { get; set; }

    private SentryPropagationContext(
        SentryId traceId,
        SpanId parentSpanId,
        bool? isSampled,
        DynamicSamplingContext? dynamicSamplingContext)
    {
        TraceId = traceId;
        SpanId = SpanId.Create();
        ParentSpanId = parentSpanId;
        DynamicSamplingContext = dynamicSamplingContext;
        IsSampled = isSampled;
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

            dynamicSamplingContext?.Items.TryGetValue("sampled", out var isSampled);
            return new SentryPropagationContext(traceHeader.TraceId, traceHeader.SpanId, traceHeader.IsSampled, dynamicSamplingContext);
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
