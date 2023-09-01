using Sentry.Extensibility;

namespace Sentry;

internal class SentryPropagationContext
{
    public SentryId TraceId { get; }
    public SpanId SpanId { get; }
    public SpanId? ParentSpanId { get; }

    internal DynamicSamplingContext? _dynamicSamplingContext;

    public DynamicSamplingContext GetDynamicSamplingContext(SentryOptions options) => _dynamicSamplingContext ??= this.CreateDynamicSamplingContext(options);

    internal SentryPropagationContext(
        SentryId traceId,
        SpanId parentSpanId,
        DynamicSamplingContext? dynamicSamplingContext = null)
    {
        TraceId = traceId;
        SpanId = SpanId.Create();
        ParentSpanId = parentSpanId;
        _dynamicSamplingContext = dynamicSamplingContext;
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
