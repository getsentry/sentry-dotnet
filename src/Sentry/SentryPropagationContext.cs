using Sentry.Extensibility;

namespace Sentry;

internal class SentryPropagationContext
{
    public SentryId TraceId { get; }
    public SpanId SpanId { get; }
    public SpanId? ParentSpanId { get; }

    private DynamicSamplingContext? _dynamicSamplingContext;
    public DynamicSamplingContext? DynamicSamplingContext
    {
        get => _dynamicSamplingContext;
        set
        {
            if (_dynamicSamplingContext is null)
            {
                _dynamicSamplingContext = value;
            }
            else
            {
                throw new Exception("Attempted to set the DynamicSamplingContext but the context exists already.");
            }
        }
    }

    internal SentryPropagationContext(
        SentryId traceId,
        SpanId parentSpanId,
        DynamicSamplingContext? dynamicSamplingContext = null)
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
        logger?.LogDebug("Creating a propagation context from headers.");

        if (traceHeader == null)
        {
            logger?.LogInfo("Sentry trace header is null. Creating new Sentry Propagation Context.");
            return new SentryPropagationContext();
        }

        var dynamicSamplingContext = baggageHeader?.CreateDynamicSamplingContext();
        return new SentryPropagationContext(traceHeader.TraceId, traceHeader.SpanId, dynamicSamplingContext);
    }
}
