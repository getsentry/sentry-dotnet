using Sentry.Extensibility;
using Sentry.Internal;

namespace Sentry;

internal class SentryPropagationContext
{
    public SentryId TraceId { get; }
    public SpanId SpanId { get; }
    public SpanId? ParentSpanId { get; }

    internal DynamicSamplingContext? _dynamicSamplingContext;

    internal SentryId? GetReplayId()
    {
        return _dynamicSamplingContext?.Items.TryGetValue("replay_id", out var value) == true
            ? SentryId.Parse(value)
            : ReplaySession.GetReplayId();
    }

    public DynamicSamplingContext GetOrCreateDynamicSamplingContext(SentryOptions options)
    {
        if (_dynamicSamplingContext is null)
        {
            options.LogDebug("Creating the Dynamic Sampling Context from the Propagation Context");
            _dynamicSamplingContext = this.CreateDynamicSamplingContext(options);
        }

        return _dynamicSamplingContext;
    }

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

    public SentryPropagationContext(SentryPropagationContext? other)
    {
        TraceId = other?.TraceId ?? SentryId.Create();
        SpanId = other?.SpanId ?? SpanId.Create();
        ParentSpanId = other?.ParentSpanId;

        _dynamicSamplingContext = other?._dynamicSamplingContext;
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
