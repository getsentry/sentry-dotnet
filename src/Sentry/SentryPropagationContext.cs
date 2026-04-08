using Sentry.Extensibility;
using Sentry.Internal;

namespace Sentry;

internal class SentryPropagationContext : IPropagationContext
{
    public SentryId TraceId { get; }
    public SpanId SpanId { get; }
    public SpanId? ParentSpanId { get; }

    public DynamicSamplingContext? DynamicSamplingContext { get; private set; }

    public DynamicSamplingContext GetOrCreateDynamicSamplingContext(SentryOptions options, IReplaySession replaySession)
    {
        if (DynamicSamplingContext is null)
        {
            options.LogDebug("Creating the Dynamic Sampling Context from the Propagation Context.");
            DynamicSamplingContext = this.CreateDynamicSamplingContext(options, replaySession);
        }

        return DynamicSamplingContext;
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

    public SentryPropagationContext(IPropagationContext? other)
    {
        TraceId = other?.TraceId ?? SentryId.Create();
        SpanId = other?.SpanId ?? SpanId.Create();
        ParentSpanId = other?.ParentSpanId;

        DynamicSamplingContext = other?.DynamicSamplingContext;
    }

    public static SentryPropagationContext CreateFromHeaders(IDiagnosticLogger? logger, SentryTraceHeader? traceHeader, BaggageHeader? baggageHeader, IReplaySession replaySession)
    {
        logger?.LogDebug("Creating a propagation context from headers.");

        if (traceHeader == null)
        {
            logger?.LogInfo("Sentry trace header is null. Creating new Sentry Propagation Context.");
            return new SentryPropagationContext();
        }

        var dynamicSamplingContext = baggageHeader?.CreateDynamicSamplingContext(replaySession);
        return new SentryPropagationContext(traceHeader.TraceId, traceHeader.SpanId, dynamicSamplingContext);
    }
}
